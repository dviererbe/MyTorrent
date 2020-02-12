using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTorrent.DistributionServices;
using MyTorrent.gRPC;
using MyTorrent.HashingServiceProviders;
using MyTorrent.FragmentStorageProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using System.IO;

using static Utils.StringEqualsIgnoreCaseExtension;

namespace MyTorrent.TrackerServer.Services
{
    internal class FileState
    {
        public UploadStatus UploadStatus { get; set; }

        //TODO: CHECK nullable realy needed?
        public IStorageSpaceAllocationToken? AllocationToken { get; set; }         
    }

    internal class GrpcTrackerServiceOptions
    {
        public string[] EndPoints { get; set; } = Array.Empty<string>() ;
    }

    internal class GrpcTrackerService : TrackerService.TrackerServiceBase, IHostedService, IDisposable
    {
        public const int DefaultPort = 50051;

        private readonly ILogger _logger;
        private readonly IEventIdCreationSource _eventIdCreationSource;

        private bool _started = false;
        private readonly SemaphoreSlim _lock;

        private readonly IHostApplicationLifetime _appLifetime;

        private readonly IHashingServiceProvider _hashingServiceProvider;
        private readonly IDistributionServicePublisher _distributionService;
        private readonly IFragmentStorageProvider _fragmentStorageProvider;

        private readonly Server _grpcServer;

        private readonly Dictionary<string, FileState> _pendingFiles;

        public GrpcTrackerService(ILogger<GrpcTrackerService> logger,
                                  IEventIdCreationSource eventIdCreationSource,
                                  IHostApplicationLifetime appLifetime,
                                  IHashingServiceProvider hashingServiceProvider,
                                  IDistributionServicePublisher distributionService,
                                  IFragmentStorageProvider fragmentStorageProvider,
                                  IOptions<GrpcTrackerServiceOptions>? options = null)
        {   
            _logger = logger;
            _eventIdCreationSource = eventIdCreationSource;

            _lock = new SemaphoreSlim(1);

            _appLifetime = appLifetime;
            
            _distributionService = distributionService;
            _hashingServiceProvider = hashingServiceProvider;
            _fragmentStorageProvider = fragmentStorageProvider;

            _pendingFiles = new Dictionary<string, FileState>();

            _grpcServer = new Server
            {
                Services = { TrackerService.BindService(this) }
            };

            if (options?.Value == null)
            {
                _logger.LogWarning("No options specified! Using default configuration.");
                _grpcServer.Ports.Add("localhost", DefaultPort, ServerCredentials.Insecure);
            }
            else
            {
                if (options.Value.EndPoints == null || options.Value.EndPoints.Length == 0)
                {
                    _logger.LogWarning("No grpc endpoints specified! Using default configuration.");
                    _grpcServer.Ports.Add("localhost", DefaultPort, ServerCredentials.Insecure);
                }
                else
                {
                    foreach (var address in options.Value.EndPoints)
                    {
                        Uri uri = new Uri(address);

                        if (!uri.Scheme.EqualsIgnoreCase("grpc"))
                            throw new NotSupportedException($"Scheme '{uri.Scheme}' is not supported! Use Scheme 'grpc'");

                        _grpcServer.Ports.Add(uri.Host, uri.Port >= 0 ? uri.Port : DefaultPort, ServerCredentials.Insecure);
                    }
                }
            }
        }

        private EventId GetNextEventId(string? name = null) => _eventIdCreationSource.GetNextId(name);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            EventId eventId = GetNextEventId();

            _logger.LogInformation(eventId, "StartAsync has been called.");

            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            try
            {
                _grpcServer.Start();
                _started = true;

                _logger.LogInformation(eventId, "Started gRPC Server.");

                string endpoints = "";

                foreach(var endpoint in _grpcServer.Ports)
                {
                    endpoints += $"{endpoint.Host}:{endpoint.Port}; ";
                }

                _logger.LogInformation(eventId, "gRPC Endpoints: " + endpoints);
            }
            catch (Exception exception)
            {
                _logger.LogError(eventId, exception, "Failed to start gRPC Server.");

                return Task.FromException(exception);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            EventId eventId = GetNextEventId();

            _logger.LogInformation(eventId, "StopAsync has been called.");

            if (_started)
            {
                _logger.LogInformation(eventId, "Shutting down gRPC Server.");

                return _grpcServer.ShutdownAsync();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Perform post-startup activities here
        /// </summary>
        private void OnStarted()
        {
            EventId eventId = GetNextEventId();

            _logger.LogInformation(eventId, "OnStarted has been called.");

        }

        /// <summary>
        /// Perform on-stopping activities here
        /// </summary>
        private void OnStopping()
        {
            EventId eventId = GetNextEventId();

            _logger.LogInformation(eventId, "OnStopping has been called.");
        }

        /// <summary>
        /// Perform post-stopped activities here
        /// </summary>
        private void OnStopped()
        {
            EventId eventId = GetNextEventId();

            _logger.LogInformation(eventId, "OnStopped has been called.");
        }

        public override Task<NetworkInfoResponse> GetNetworkInfo(NetworkInfoRequest request, ServerCallContext context)
        {
            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, $"'{context.Peer}' requested Network-Info.");

            NetworkInfoResponse response = new NetworkInfoResponse
            {
                FragmentSize = _distributionService.FragmentSize,
                HashAlgorithm = _hashingServiceProvider.AlgorithmName,
                TorrentServer = { _distributionService.DistributionEndPoints.Select(uri => uri.AbsoluteUri) }
            };

            return Task.FromResult(response);
        }

        public override async Task<UploadStatusResponse> GetUploadStatus(UploadStatusRequest request, ServerCallContext context)
        {
            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, $"'{context.Peer}' requested Upload-Status.");

            if (!_hashingServiceProvider.Validate(request.FileHash))
            {
                _logger.LogError(eventId, "Invalid file hash format!");
#if TRACE
                _logger.LogTrace(eventId, "Requested File-Hash: " + (request.FileHash == null ? "null" : $"'{request.FileHash}'"));
#endif
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid file hash format."));
            }

            string normalizedFileHash = _hashingServiceProvider.Normalize(request.FileHash);
            _logger.LogDebug(eventId, "Requested File-Hash: " + normalizedFileHash);

            UploadStatusResponse response = new UploadStatusResponse
            {
                Status = UploadStatus.Unknown
            };

            try
            {
                await _lock.WaitAsync();

                if (_pendingFiles.TryGetValue(normalizedFileHash, out FileState? fileState))
                {
                    response.Status = fileState!.UploadStatus;
                }
                else if (_distributionService.ExistsFile(normalizedFileHash))
                {
                    response.Status = UploadStatus.Distributed;
                }
            }
            finally
            {
                _lock.Release();
            }
#if TRACE
            _logger.LogTrace(eventId, "Response: Upload Status: " + response.Status);
#endif
            return response;
        }

        public override async Task<FileUploadInitiationResponse> InitiateUpload(FileUploadInitiationRequest request, ServerCallContext context)
        {
            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, $"'{context.Peer}' requests upload initiation.");

            if (request.FileSize < 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "FileSize can't be negative."));
            else if (request.FileSize == 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "FileSize can't be zero."));

            if (!_hashingServiceProvider.Validate(request.FileHash))
            {
                _logger.LogError(eventId, "Invalid file hash format!");
#if TRACE
                _logger.LogTrace(eventId, "Requested File-Hash: " + (request.FileHash == null ? "null" : $"'{request.FileHash}'"));
#endif
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid file hash format."));
            }

            string normalizedFileHash = _hashingServiceProvider.Normalize(request.FileHash);
            _logger.LogDebug(eventId, "Requested File-Hash: " + normalizedFileHash);
            _logger.LogDebug(eventId, "Requested File-Size: " + request.FileSize);

            try
            {
                await _lock.WaitAsync();

                if (_pendingFiles.TryGetValue(normalizedFileHash, out FileState? fileState))
                {
                    if (fileState!.UploadStatus != UploadStatus.Canceled)
                    {
                        throw new RpcException(new Status(StatusCode.AlreadyExists, "File with the specified hash was already initiated.")); 
                    }
                }
                else if (_distributionService.ExistsFile(normalizedFileHash))
                {
                    throw new RpcException(new Status(StatusCode.AlreadyExists, "File with the specified hash is already distributed."));
                }

                var allocationToken = await _fragmentStorageProvider.AllocateStorageSpaceAsync(request.FileSize);

                bool add = false;

                if (fileState == null)
                {
                    fileState = new FileState();
                    add = true;
                }

                fileState.AllocationToken = allocationToken;
                fileState.UploadStatus = UploadStatus.Initiated;

                if (add)
                    _pendingFiles.Add(normalizedFileHash, fileState);                
            }
            catch (IOException)
            {
                throw new RpcException(new Status(StatusCode.ResourceExhausted, "Failed to allocate enough space for the specified file."));
            }
            finally
            {
                _lock.Release();
            }

            return new FileUploadInitiationResponse();
        }

        public override async Task<FileUploadResponse> UploadFileFragments(IAsyncStreamReader<FileFragment> requestStream, ServerCallContext context)
        { 
            string error;
            EventId eventId = GetNextEventId();

            _logger.LogDebug(eventId, $"'{context.Peer}' started uploading file fragments");

            List<string> fragmentOrder = new List<string>();
            Dictionary<string, FragmentHolderList> distributionMap = new Dictionary<string, FragmentHolderList>();

            //waiting for first fragment
            if (await requestStream.MoveNext().ConfigureAwait(false)) 
            {
                if (!_hashingServiceProvider.Validate(requestStream.Current.FileHash))
                {
                    _logger.LogError(eventId, error = "Invalid file hash format.");
#if TRACE
                    _logger.LogTrace(eventId, "Requested File-Hash: " + (requestStream.Current.FileHash == null ? "null" : $"'{requestStream.Current.FileHash}'"));
#endif
                    throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                }

                string normalizedFileHash = _hashingServiceProvider.Normalize(requestStream.Current.FileHash);
                _logger.LogDebug(eventId, "File-Hash: " + normalizedFileHash);

                FileState fileState;

                try
                {
                    await _lock.WaitAsync().ConfigureAwait(false);

#pragma warning disable CS8600 // Das NULL-Literal oder ein möglicher NULL-Wert wird in einen Nicht-Nullable-Typ konvertiert.
                    if (!_pendingFiles.TryGetValue(normalizedFileHash, out fileState))
#pragma warning restore CS8600 // Das NULL-Literal oder ein möglicher NULL-Wert wird in einen Nicht-Nullable-Typ konvertiert.
                    {
                        _logger.LogError(eventId, error = "File upload for the specified file was not initiated.");
                        throw new RpcException(new Status(StatusCode.NotFound, error));
                    }

                    if (fileState.UploadStatus != UploadStatus.Initiated)
                    {
                        _logger.LogError(eventId, error = "Upload state for the specified file is not initiated.");
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, error));
                    }

                    fileState.UploadStatus = UploadStatus.Uploading;
                }
                finally
                {
                    _lock.Release();
                }

                await using IStorageSpaceAllocationToken? allocationToken = fileState.AllocationToken;

                long fragmentSize = _distributionService.FragmentSize;
                long bytesLeft = allocationToken!.TotalAllocatedStorageSpace;

                IIncrementalHashCalculator hashCalculator = _hashingServiceProvider.GetIncrementalHashCalculator();

                try
                {
                    do
                    {
                        FileFragment fragment = requestStream.Current;

                        //Check specified File Hash
                        if (!normalizedFileHash.EqualsIgnoreCase(fragment.FileHash))
                        {
                            fileState.UploadStatus = UploadStatus.Canceled;
                            _logger.LogError(eventId, error = "Specified hash value of the fragment packets are not consistent!");
                            throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                        }

                        //Validate Fragment Hash
                        if (!_hashingServiceProvider.Validate(fragment.FragmentHash))
                        {
                            fileState.UploadStatus = UploadStatus.Canceled;
                            _logger.LogError(eventId, error = "Invalid fragment hash format.");
                            throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                        }

                        //Normalize Fragment Hash
                        fragment.FragmentHash = _hashingServiceProvider.Normalize(fragment.FragmentHash);

                        byte[] data = fragment.Data.ToByteArray();

                        //Check Fragment Size
                        if (data.LongLength != fragmentSize)
                        {
                            if (data.LongLength == 0L)
                            {
                                fileState.UploadStatus = UploadStatus.Canceled;
                                _logger.LogError(eventId, error = "Send empty fragment");
                                throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                            }
                            else if (data.LongLength > fragmentSize)
                            {
                                fileState.UploadStatus = UploadStatus.Canceled;
                                _logger.LogError(eventId, error = "Fragment size to large.");
                                throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                            }
                            else if (bytesLeft >= fragmentSize)
                            {
                                fileState.UploadStatus = UploadStatus.Canceled;
                                _logger.LogError(eventId, error = "Fragment size to small.");
                                throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                            }
                            else
                            {
                                if (data.LongLength < bytesLeft)
                                {
                                    fileState.UploadStatus = UploadStatus.Canceled;
                                    _logger.LogError(eventId, error = "Fragment size to small.");
                                    throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                                }
                                else if (data.LongLength > bytesLeft)
                                {
                                    fileState.UploadStatus = UploadStatus.Canceled;
                                    _logger.LogError(eventId, error = "Fragment size to large.");
                                    throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                                }
                            }
                        }

                        //Validate byte content
                        if (!fragment.FragmentHash.EqualsIgnoreCase(_hashingServiceProvider.ComputeHash(data)))
                        {
                            fileState.UploadStatus = UploadStatus.Canceled;
                            _logger.LogError(eventId, error = "Fragment hash does not match content of the byte data.");
                            throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                        }

                        //Store Fragment
                        try
                        {
                            if (!await _fragmentStorageProvider.IsFragmentStoredAsync(fragment.FragmentHash))
                                await _fragmentStorageProvider.StoreFragmentAsync(fragment.FragmentHash, data, allocationToken);
                        }
                        catch (Exception exception)
                        {
                            fileState.UploadStatus = UploadStatus.Canceled;
                            _logger.LogError(eventId, exception, error = "Failed to store data.");
                            throw new RpcException(new Status(StatusCode.Internal, error));
                        }

                        fragmentOrder.Add(fragment.FragmentHash);
                        bytesLeft -= data.Length;

                        hashCalculator.AppendData(data);
                    }
                    while (await requestStream.MoveNext().ConfigureAwait(false));
                }
                catch (Exception exception)
                {
                    fileState.UploadStatus = UploadStatus.Canceled;
                    _logger.LogError(eventId, exception, error = $"Internal error ({exception.GetType().FullName}): '{exception.Message}'");
                    throw new RpcException(new Status(StatusCode.Internal, error));
                }

                fileState.UploadStatus = UploadStatus.Validating;

                if (!normalizedFileHash.EqualsIgnoreCase(hashCalculator.GetHashAndReset()))
                {
                    fileState.UploadStatus = UploadStatus.Canceled;
                    _logger.LogError(eventId, error = "File hash value does not match the transmitted data.");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                }

                fileState.UploadStatus = UploadStatus.Distributing;

                try
                {
                    await _distributionService.PublishFileInfoAsync(normalizedFileHash, allocationToken.TotalAllocatedStorageSpace, fragmentOrder);
                }
                catch (Exception exception)
                {
                    fileState.UploadStatus = UploadStatus.Canceled;
                    _logger.LogError(eventId, exception, error = "Failed to publish file info to distribution network.");
                    throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                }

                IEnumerable<string>[] distributions;
                
                try
                {
                    Task<IEnumerable<string>>[] distributionTasks = new Task<IEnumerable<string>>[fragmentOrder.Count];

                    for (int i = 0; i < fragmentOrder.Count; ++i)
                    {
                        if (_distributionService.TryGetFragmentDistribution(fragmentOrder[i], out IEnumerable<Uri> fragmentUris))
                        {
                            distributionTasks[i] = Task.FromResult(fragmentUris.Select(uri => uri.AbsoluteUri));
                        }
                        else
                        {
                            distributionTasks[i] = _distributionService.DistributeFragmentAsync(
                                    fragmentHash: fragmentOrder[i],
                                    fragmentStream: _fragmentStorageProvider.ReadFragment(fragmentOrder[i], true))
                                .ContinueWith(task => task.Result.Select(uri => uri.AbsoluteUri));
                        }
                    }

                    distributions = await Task.WhenAll(distributionTasks).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    fileState.UploadStatus = UploadStatus.Canceled;
                    _logger.LogError(eventId, exception, error = "Failed to distribute one ore more file fragments to the distribution network.");
                    throw new RpcException(new Status(StatusCode.Internal, error));
                }

                for (int i = 0; i < fragmentOrder.Count; ++i)
                {
                    FragmentHolderList list = new FragmentHolderList()
                    {
                        EndPoints = { distributions[i] }
                    };

                    distributionMap.Add(fragmentOrder[i], list);
                } 

                _pendingFiles.Remove(normalizedFileHash);
            }
            else
            {
                _logger.LogError(error = "No fragment was transmitted.");
                throw new RpcException(new Status(StatusCode.OutOfRange, error));
            }

            var result = new FileUploadResponse()
            {
                FragmentDistribution = { distributionMap }
            };

            return result;
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}

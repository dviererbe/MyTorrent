using MyTorrent.gRPC;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using MyTorrent.DistributionServices;
using System.Collections.Generic;
using System.Linq;

using static Utils.StringEqualsIgnoreCaseExtension;
using MyTorrent.FragmentStorageProviders;
using Google.Protobuf;

namespace MyTorrent.TorrentServer.Services
{
    internal class GrpcTorrentServiceOptions
    {
        public string[] EndPoints { get; set; } = Array.Empty<string>();
    }

    internal class GrpcTorrentService : TorrentService.TorrentServiceBase, IHostedService, IDisposable
    {
        public const int DefaultPort = 50051;

        private readonly ILogger _logger;
        private readonly IEventIdCreationSource _eventIdCreationSource;

        private bool _started = false;

        private readonly IHostApplicationLifetime _appLifetime;

        private readonly IDistributionServiceSubscriber _distributionService;

        private readonly Server _grpcServer;

        public GrpcTorrentService(ILogger<GrpcTorrentService> logger,
                                  IEventIdCreationSource eventIdCreationSource,
                                  IHostApplicationLifetime appLifetime,
                                  IDistributionServiceSubscriber distributionService,
                                  IOptions<GrpcTorrentServiceOptions>? options = null)
        {
            _logger = logger;
            _eventIdCreationSource = eventIdCreationSource;

            _appLifetime = appLifetime;
            _distributionService = distributionService;

            _grpcServer = new Server
            {
                Services = { TorrentService.BindService(this) }
            };

            if (options == null || options.Value == null)
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            EventId eventId = GetNextEventId();

            _logger.LogInformation(eventId, "StartAsync has been called.");

            try
            {
                await _distributionService.InitializeAsync(_grpcServer.Ports.Select(port => new Uri($"tcp://{port.Host}:{port.Port}"))).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(eventId, exception, "Error occured on distribution service initialization.");
            }

            try
            {
                _grpcServer.Start();
                _started = true;

                _logger.LogInformation(eventId, "Started gRPC Server.");

                string endpoints = "";

                foreach (var endpoint in _grpcServer.Ports)
                {
                    endpoints += $"{endpoint.Host}:{endpoint.Port}; ";
                }

                _logger.LogInformation(eventId, "gRPC Endpoints: " + endpoints);
            }
            catch (Exception exception)
            {
                _logger.LogError(eventId, exception, "Failed to start gRPC Server.");

                throw;
            }
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

        public override Task<FileDistributionResponse> GetFileDistribution(FileDistributionRequest request, ServerCallContext context)
        {
            FileDistributionResponse Operation()
            {
                EventId eventId = GetNextEventId();
#if DEBUG
                _logger.LogDebug(eventId, $"'{context.Peer}' requested file distribution (Hash: '{request.FileHash}').");
#endif
                if (!_distributionService.TryGetFileInfo(request.FileHash, out IFragmentedFileInfo? fileInfo))
                {
                    string errorMessage = $"Fragmented file (Hash: '{request.FileHash}') not found.";
                    _logger.LogError(eventId, errorMessage);
                    throw new RpcException(new Status(StatusCode.NotFound, errorMessage));
                }

                var response = new FileDistributionResponse()
                {
                    FragmentOrder = { fileInfo.FragmentSequence }
                };

                Random random = new Random();
                List<string> randomOrderedFragmentUris = new List<string>();

                foreach (string fragmentHash in fileInfo.FragmentSequence)
                {
                    if (!_distributionService.TryGetFragmentDistribution(fragmentHash, out IEnumerable<Uri> fragmentUris))
                    {
                        string errorMessage = $"Fragment (Hash: '{request.FileHash}') not found which was part of the fragment sequence of the fragmented file.";
                        _logger.LogError(eventId, errorMessage);
                        throw new RpcException(new Status(StatusCode.OutOfRange, errorMessage));
                    }

                    int exclusiveUpperBound = 0;
                    randomOrderedFragmentUris.Clear();

                    foreach (Uri fragmentUri in fragmentUris)
                    {
                        randomOrderedFragmentUris.Insert(random.Next(0, ++exclusiveUpperBound), fragmentUri.AbsoluteUri);
                    }

                    FragmentHolderList fragmentHolders = new FragmentHolderList()
                    {
                        EndPoints = { randomOrderedFragmentUris }
                    };

                    response.FragmentDistribution.Add(fragmentHash, fragmentHolders);
                }

                return response;
            }

            return Task.Run(Operation);
        }

        public override async Task<FragmentDownloadResponse> DownloadFileFragment(FragmentDownloadRequest request, ServerCallContext context)
        {
            EventId eventId = GetNextEventId();
            IFragmentStorageProvider storageProvider = _distributionService.FragmentStorage;
#if DEBUG
            _logger.LogDebug(eventId, $"'{context.Peer}' is downloading files.");
#endif
            try
            {
#if TRACE
                _logger.LogTrace(eventId,
                    $"'{context.Peer}' is requested fragment with hash: '{request.FragmentHash}'.");
#endif
                byte[] data = await storageProvider.GetFragmentAsync(request.FragmentHash);

                FragmentDownloadResponse response = new FragmentDownloadResponse
                {
                    Data = ByteString.CopyFrom(data)
                };

                return response;
            }
            catch (FormatException exception)
            {
                string errorMessage = $"Invalid Fragment Hash ('{request.FragmentHash}') received.";
                _logger.LogError(eventId, exception, errorMessage);
                throw new RpcException(new Status(StatusCode.InvalidArgument, errorMessage));
            }
            catch (KeyNotFoundException exception)
            {
                string errorMessage = $"Fragment (Hash: '{request.FragmentHash}') not found.";
                _logger.LogError(eventId, exception, errorMessage);
                throw new RpcException(new Status(StatusCode.NotFound, errorMessage));
            }
            catch (Exception exception)
            {
                string errorMessage = $"Failed to retrive fragment (Hash: '{request.FragmentHash}').";
                _logger.LogError(eventId, exception, errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }
        }

        public void Dispose()
        {
            
        }
    }
}

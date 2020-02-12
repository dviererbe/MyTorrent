using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Server;
using MyTorrent.HashingServiceProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An <see cref="IDistributionServiceSubscriber"/> implementation that uses MQTT as the underlying distribution communication protocol.
    /// </summary>
    public class MqttDistributionServicePublisher : IDistributionServicePublisher
    {
        #region Private Variables
        
        //TODO: IMPLEMENT Log Messages
        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<MqttDistributionServicePublisher> _logger;

        private readonly IHashingServiceProvider _hashingServiceProvider;

        private readonly long _fragmentSize;
        private readonly DistributionMap _distributionMap;

        private IMqttServer? _mqttServer;

        private volatile bool _disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="MqttDistributionServicePublisher"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger that should be used for this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        /// <param name="eventIdCreationSource">
        /// The source for creating unique event Id's that should be used by this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        /// <param name="hashingServiceProvider">
        /// The service provider that validates und normalizes hashes and should be used by this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        /// <param name="options">
        /// The options to configure this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        public MqttDistributionServicePublisher(
            ILogger<MqttDistributionServicePublisher> logger,
            IEventIdCreationSource eventIdCreationSource,
            IHashingServiceProvider hashingServiceProvider,
            IOptions<MqttDistributionServicePublisherOptions>? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventIdCreationSource = eventIdCreationSource ?? throw new ArgumentNullException(nameof(eventIdCreationSource));
            _hashingServiceProvider = hashingServiceProvider ?? throw new ArgumentNullException(nameof(hashingServiceProvider)); ;

            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, "Initializing Mqtt Distribution-Service-Publisher.");

            _distributionMap = new DistributionMap();

            options ??= Options.Create(MqttDistributionServicePublisherOptions.Default);
            
            _fragmentSize = options.Value?.FragmentSize ?? MqttDistributionServicePublisherOptions.Default.FragmentSize;

            int port = options.Value?.Port ?? MqttDistributionServicePublisherOptions.Default.Port;
            
            if (port < 0x0000 || port > 0xffff)
                throw new ArgumentOutOfRangeException(nameof(MqttDistributionServicePublisherOptions.Port), port, "Invalid port number.");

            _ = StartServerAsync(port, eventId);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServicePublisher" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~MqttDistributionServicePublisher()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the size of a whole fragment in bytes.
        /// </summary>
        /// <remarks>
        /// No Fragment is allowed to be larger than this <see cref="FragmentSize"/>, but 
        /// the last fragment of an file is allowed to be smaller, but not empty.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public long FragmentSize 
        { 
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return _fragmentSize;
            }
        }

        /// <summary>
        /// Gets the uris of the endpoints where the fragments are distributed to and can be retrived from.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public IReadOnlyCollection<Uri> DistributionEndPoints
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return _distributionMap.Endpoints;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the next unique event id.
        /// </summary>
        /// <param name="name">
        /// The name of the event (optional).
        /// </param>
        /// <returns>
        /// The unique event id.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventId GetNextEventId(string? name = null) => _eventIdCreationSource.GetNextId(name);

        /// <summary>
        /// Ensures that the distribution service publisher was not disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this distribution service publisher was disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStorageProviderWasNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    objectName: GetType().FullName,
                    message: "Mqtt distribution service publisher  was already disposed.");
        }

        /// <summary>
        /// Ensures that a specific hash value is valid and normalizes it if it is valid.
        /// </summary>
        /// <param name="hashValue">
        /// The hash value to validate and normalize.
        /// </param>
        /// <param name="errorMessage">
        /// The error message that the <see cref="FormatException"/> should contain if the hash value is not valid.
        /// </param>
        /// <exception cref="FormatException">
        /// Specified <paramref name="hashValue"/> is not valid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureHashValueIsValidAndNormalize(ref string hashValue, string errorMessage = "Invalid hash format.")
        {
            if (!_hashingServiceProvider.Validate(hashValue))
            {
                throw new FormatException(errorMessage)
                {
                    Data = { { "HashValue", hashValue } }
                };
            }

            hashValue = _hashingServiceProvider.Normalize(hashValue);
        }

        /// <summary>
        /// Tries to validate a given hash value and normalizes it if it is valid.
        /// </summary>
        /// <param name="hashValue">
        /// The hash value to validate and normalize.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the hash value is valid and was normalized; otherwise <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryValidateAndNormalizeHashValue(ref string hashValue)
        {
            if (_hashingServiceProvider.Validate(hashValue))
            {
                hashValue = _hashingServiceProvider.Normalize(hashValue);
                return true;
            }

            return false;
        }

        private async Task StartServerAsync(int port, EventId eventId)
        {
            try
            {
                var optionsBuilder = new MqttServerOptionsBuilder()
                        .WithoutDefaultEndpoint()
                        .WithoutEncryptedEndpoint()
                        .WithConnectionBacklog(100)
                        .WithDefaultEndpointPort(port);

                var mqttServer = new MqttFactory().CreateMqttServer();
                await mqttServer.StartAsync(optionsBuilder.Build());

                _mqttServer = mqttServer;

                _logger.LogInformation(eventId, $"Started MQTT Server on port {port}.");
            }
            catch (Exception exception)
            {
                _logger.LogCritical(eventId, exception, "Failed to start MQTT Server!");
            }
        }

        #endregion

        /// <summary>
        /// Checks if the distribution network contains a file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the file to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution network conatins a file with the 
        /// specified <paramref name="fileHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool ExistsFile(string fileHash)
        {
            EnsureStorageProviderWasNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fileHash))
                return _distributionMap.ContainsFile(fileHash);

            return false;
        }

        /// <summary>
        /// Checks if the distribution network contains a fragment with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the fragment to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution network conatins a fragment with the 
        /// specified <paramref name="fragmentHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool ExistsFragment(string fragmentHash)
        {
            EnsureStorageProviderWasNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fragmentHash))
                return _distributionMap.ContainsFragment(fragmentHash);

            return false;
        }

        /// <summary>
        /// Tries to get the information about an fragmented file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the content of the file to get the information for.
        /// </param>
        /// <param name="fileInfo">
        /// When this method returns, contains the <see cref="IFragmentedFileInfo"/> about the file with the
        /// specified <paramref name="fileHash"/>, if the operation succeeded, or <see langword="null"/> if the 
        /// operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The operation succeeds if information about the specified file was published to the distribution network.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool TryGetFileInfo(string fileHash, [NotNullWhen(true)] out IFragmentedFileInfo? fileInfo)
        {
            EnsureStorageProviderWasNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fileHash))
            {
                if (_distributionMap.TryGetFragmentedFileInfo(fileHash, out FragmentedFileInfo? fragmentedFileInfo))
                {
                    fileInfo = fragmentedFileInfo;
                    return true;
                }
            }

            fileInfo = null;
            return false;
        }

        /// <summary>
        /// Tries to get the uris of an fragment with a specific hash value where the fragment was distributed to.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the content of the fragment to get the information for.
        /// </param>
        /// <param name="fragmentUris">
        /// When this method returns, contains the enumeration of uris where the fragment with the 
        /// specified <paramref name="fragmentHash"/> was distributed to, if the operation succeeded, or 
        /// an empty array if the operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The operation succeeds if information about the specified fragment was published to the distribution network.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool TryGetFragmentDistribution(string fragmentHash, out IEnumerable<Uri> fragmentUris)
        {
            EnsureStorageProviderWasNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fragmentHash))
            {
                if (_distributionMap.TryGetClientsWithFragment(fragmentHash, out IEnumerable<IClient> clientsWithFragment))
                {
                    List<Uri> fragmentUriList = new List<Uri>();

                    foreach (IClient client in clientsWithFragment)
                    {
                        foreach (Uri clientEndpoint in client.Endpoints)
                        {
                            fragmentUriList.Add(new Uri(clientEndpoint, fragmentHash));
                        }
                    }

                    fragmentUris = fragmentUriList;
                    return true;
                }
            }

            fragmentUris = Array.Empty<Uri>();
            return false;
        }

        /// <summary>
        /// Asynchronously publishes information about an fragmented file.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the entire file content of the fragmented file whose information should be published.
        /// </param>
        /// <param name="fileSize">
        /// The size of the entire file content of the fragmented file whose information should be published.
        /// </param>
        /// <param name="fragmentHashSequence">
        /// Sequence of hash values of the fragments the file consists of. 
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileHash"/>, <paramref name="fragmentHashSequence"/> or one of 
        /// the elements of <paramref name="fragmentHashSequence"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="fileSize"/> is negative or zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An file with the specified <paramref name="fileHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fileHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to publish the file info, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public async Task PublishFileInfoAsync(string fileHash, long fileSize, IEnumerable<string> fragmentHashSequence, CancellationToken? cancellationToken = null)
        {
            EnsureStorageProviderWasNotDisposed();

            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            if (fileHash == null)
                throw new ArgumentNullException(nameof(fileHash));
            
            if (fragmentHashSequence == null)
                throw new ArgumentNullException(nameof(fileHash));

            if (fileSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(fileSize), fileSize, "File size hash to be positive.");

            EnsureHashValueIsValidAndNormalize(ref fileHash);


            //TODO: publish file info to network
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously distributes a file fragment to the distribution network.
        /// </summary>
        /// <param name="fragmentHash">
        /// Hash value of the file fragment that should be distributed.
        /// </param>
        /// <param name="fragmentData">
        /// Content of the file fragment that should be distributed.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous distrubution operation and wraps the 
        /// uris of the fragment where it was distributed to.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fragmentHash"/> or <paramref name="fragmentData"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Length of the <paramref name="fragmentData"/> is larger than the maximum allowed <see cref="IDistributionServiceObserver.FragmentSize"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An fragment with the specified <paramref name="fragmentHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to distribute the fragment, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public async Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, byte[] fragmentData, CancellationToken? cancellationToken = null)
        {
            EnsureStorageProviderWasNotDisposed();

            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            if (fragmentHash == null)
                throw new ArgumentNullException(nameof(fragmentHash));

            if (fragmentData == null)
                throw new ArgumentNullException(nameof(fragmentData));

            if (fragmentData.LongLength == 0L)
                throw new ArgumentOutOfRangeException(nameof(fragmentData), fragmentData, "Fragment data is empty.");
            
            if (fragmentData.LongLength > _fragmentSize)
                throw new ArgumentOutOfRangeException(nameof(fragmentData), fragmentData, "Fragment data larger than maximum allowed fragment size.");

            EnsureHashValueIsValidAndNormalize(ref fragmentHash);


            //TODO: publish fragment to network
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously distributes a file fragment to the distribution network.
        /// </summary>
        /// <param name="fragmentHash">
        /// Hash value of the file fragment that should be distributed.
        /// </param>
        /// <param name="fragmentStream">
        /// Content of the file fragment that should be distributed, that can be read from this <see cref="Stream"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous distrubution operation and wraps the 
        /// uris of the fragment where it was distributed to.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fragmentHash"/> or <paramref name="fragmentStream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Length of the <paramref name="fragmentStream"/> is larger than the maximum allowed <see cref="IDistributionServiceObserver.FragmentSize"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An fragment with the specified <paramref name="fragmentHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to distribute the fragment, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public async Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, Stream fragmentStream, CancellationToken? cancellationToken = null)
        {
            EnsureStorageProviderWasNotDisposed();

            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            if (fragmentHash == null)
                throw new ArgumentNullException(nameof(fragmentHash));

            if (fragmentStream == null)
                throw new ArgumentNullException(nameof(fragmentStream));

            //FIXME: Can throw NotSupportedException
            if (fragmentStream.Length == 0L)
                throw new ArgumentOutOfRangeException(nameof(fragmentStream), fragmentStream, "Fragment stream is empty.");

            //FIXME: Can throw NotSupportedException
            if (fragmentStream.Length > _fragmentSize)
                throw new ArgumentOutOfRangeException(nameof(fragmentStream), fragmentStream, "Fragment stream larger than maximum allowed fragment size.");

            EnsureHashValueIsValidAndNormalize(ref fragmentHash);


            //TODO: publish fragment to network
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases all allocated resources associated to this <see cref="MqttDistributionServicePublisher" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServicePublisher" /> and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
        /// release only unmanaged resources.
        /// </param>
        public void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                _distributionMap.Clear();
            }

            //TODO: stop mqtt server;

            //TODO:
            throw new NotImplementedException();
        }
    }
}

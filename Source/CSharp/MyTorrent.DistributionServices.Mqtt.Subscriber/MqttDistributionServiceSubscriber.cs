using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTorrent.FragmentStorageProviders;
using MyTorrent.HashingServiceProviders;

namespace MyTorrent.DistributionServices
{

    /// <summary>
    /// An <see cref="IDistributionServiceSubscriber"/> implementation that uses MQTT as the underlying distribution communication protocol.
    /// </summary>
    public class MqttDistributionServiceSubscriber : IDistributionServiceSubscriber
    {
        /// <summary>
        /// Identifier used by the mqtt client.
        /// </summary>
        public readonly Guid ClientId = Guid.NewGuid();

        //TODO: IMPLEMENT Log Messages
        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<MqttDistributionServiceSubscriber> _logger;

        private readonly IHashingServiceProvider _hashingServiceProvider;
        private readonly IFragmentStorageProvider _fragmentStorageProvider;

        private long? _fragmentSize;
        private readonly DistributionMap _distributionMap;
        
        private volatile bool _disposed = false;

        /// <summary>
        /// Initializes a new <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger that should be used for this <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </param>
        /// <param name="eventIdCreationSource">
        /// The source for creating unique event Id's that should be used by this <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </param>
        /// <param name="hashingServiceProvider">
        /// The service provider that validates und normalizes hashes and should be used by this <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </param>
        /// <param name="fragmentStorageProvider">
        /// The storage provider where the fragments are stored to and retrived from.
        /// </param>
        /// <param name="options">
        /// The options to configure this <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </param>
        public MqttDistributionServiceSubscriber(
            ILogger<MqttDistributionServiceSubscriber> logger,
            IEventIdCreationSource eventIdCreationSource,
            IHashingServiceProvider hashingServiceProvider,
            IFragmentStorageProvider fragmentStorageProvider,
            IOptions<MqttDistributionServiceSubscriberOptions>? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventIdCreationSource = eventIdCreationSource ?? throw new ArgumentNullException(nameof(eventIdCreationSource));
            _hashingServiceProvider = hashingServiceProvider ?? throw new ArgumentNullException(nameof(hashingServiceProvider));
            _fragmentStorageProvider = fragmentStorageProvider ?? throw new ArgumentNullException(nameof(fragmentStorageProvider));

            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, "Initializing Mqtt Distribution-Service-Subscriber.");

            _distributionMap = new DistributionMap();

            /*
            _distributionMap.AddClient();

            foreach (string fragmentHash in _fragmentStorageProvider.Fragments)
            {
                
                _distributionMap.
            }
            */
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServiceSubscriber" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~MqttDistributionServiceSubscriber()
        {
            Dispose(false);
        }


        /// <summary>
        /// Gets the size of a whole fragment in bytes.
        /// </summary>
        /// <remarks>
        /// No Fragment is allowed to be larger than this <see cref="FragmentSize"/>, but 
        /// the last fragment of an file is allowed to be smaller, but not empty.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Mqtt Client has not received this information yet.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public long FragmentSize
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();

                if (_fragmentSize.HasValue)
                    return _fragmentSize.Value;

                throw new InvalidOperationException("Mqtt Client has not received this information yet.");
            }
        }

        /// <summary>
        /// Gets the uris of the endpoints where the fragments are distributed to an d can be retrived from.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public IReadOnlyCollection<Uri> DistributionEndPoints
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return _distributionMap.Endpoints;
            }
        }

        /// <summary>
        /// Gets the <see cref="IFragmentStorageProvider"/> where the fragments are stored which this <see cref="IDistributionServiceSubscriber"/> decided to hold.
        /// </summary>
        /// <remarks>
        /// An <see cref="IDistributionServiceSubscriber"/> of course don't have to hold any fragment that are published by the <see cref="IDistributionServicePublisher"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public IFragmentStorageProvider FragmentStorage 
        { 
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return _fragmentStorageProvider;
            }
        }

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
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
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
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
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
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
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
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
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
        /// Releases all allocated resources associated to this <see cref="MqttDistributionServiceSubscriber" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServiceSubscriber" /> and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
        /// release only unmanaged resources.
        /// </param>
        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                //TODO: release managed resources;
            }

            throw new NotImplementedException();
        }
    }
}

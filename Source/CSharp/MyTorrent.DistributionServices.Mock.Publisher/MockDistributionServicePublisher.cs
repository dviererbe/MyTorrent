using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// An mock implementation of the <see cref="IDistributionServiceSubscriber"/>.
    /// </summary>
    public class MockDistributionServicePublisher : IDistributionServicePublisher
    {
        private readonly ILogger<MockDistributionServicePublisher> _logger;

        private readonly long _fragmentSize;

        private volatile bool _disposed = false;

        /// <summary>
        /// Initializes a new <see cref="MockDistributionServicePublisher"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger that should be used for this <see cref="MockDistributionServicePublisher"/> instance.
        /// </param>
        /// <param name="options">
        /// The options to configure this <see cref="MockDistributionServicePublisher"/> instance.
        /// </param>
        public MockDistributionServicePublisher(
            ILogger<MockDistributionServicePublisher> logger,
            IOptions<MockDistributionServicePublisherOptions>? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            options ??= Options.Create(MockDistributionServicePublisherOptions.Default);

            _fragmentSize = options.Value?.FragmentSize ?? MockDistributionServicePublisherOptions.Default.FragmentSize;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MockDistributionServicePublisher" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~MockDistributionServicePublisher()
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
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public IReadOnlyCollection<Uri> DistributionEndPoints => new Uri [] { new Uri("gRPC://127.0.0.1:50051") };


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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool ExistsFile(string fileHash)
        {
            EnsureStorageProviderWasNotDisposed();

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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool ExistsFragment(string fragmentHash)
        {
            EnsureStorageProviderWasNotDisposed();

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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool TryGetFileInfo(string fileHash, [NotNullWhen(true)] out IFragmentedFileInfo? fileInfo)
        {
            EnsureStorageProviderWasNotDisposed();

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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool TryGetFragmentDistribution(string fragmentHash, out IEnumerable<Uri> fragmentUris)
        {
            EnsureStorageProviderWasNotDisposed();

            fragmentUris = Array.Empty<Uri>();
            return false;
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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, byte[] fragmentData, CancellationToken? cancellationToken = null)
        {
            try
            {
                EnsureStorageProviderWasNotDisposed();
            }
            catch (Exception exception)
            {
                return Task.FromException<IEnumerable<Uri>>(exception);
            }

            return Task.FromResult<IEnumerable<Uri>>(new Uri[1] { new Uri("gRPC://127.0.0.1:50051/" + fragmentHash)});
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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, Stream fragmentStream, CancellationToken? cancellationToken = null)
        {
            try
            {
                EnsureStorageProviderWasNotDisposed();
            }
            catch (Exception exception)
            {
                return Task.FromException<IEnumerable<Uri>>(exception);
            }

            return Task.FromResult<IEnumerable<Uri>>(new Uri[1] { new Uri("gRPC://127.0.0.1:50051/" + fragmentHash) });
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
        /// This method was called after the <see cref="MockDistributionServicePublisher"/> was disposed.
        /// </exception>
        public Task PublishFileInfoAsync(string fileHash, long fileSize, IEnumerable<string> fragmentHashSequence, CancellationToken? cancellationToken = null)
        {
            try
            {
                EnsureStorageProviderWasNotDisposed();
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Releases all allocated resources associated to this <see cref="MockDistributionServicePublisher" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MockDistributionServicePublisher" /> and
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
                //release managed resources here
            }
        }
    }
}

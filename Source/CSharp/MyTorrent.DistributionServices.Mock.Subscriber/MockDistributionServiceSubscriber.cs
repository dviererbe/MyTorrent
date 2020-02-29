using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTorrent.FragmentStorageProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An mock implementation of <see cref="IDistributionServiceSubscriber"/>.
    /// </summary>
    public class MockDistributionServiceSubscriber : IDistributionServiceSubscriber
    {
        private readonly ILogger<MockDistributionServiceSubscriber> _logger;

        private readonly IFragmentStorageProvider _fragmentStorageProvider;

        private long? _fragmentSize;

        private volatile bool _disposed = false;

        private readonly string fileHash;
        private readonly long fileSize;
        private List<string> _fragmentSequence = new List<string>();

        /// <summary>
        /// Initializes a new <see cref="MockDistributionServiceSubscriber"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger that should be used for this <see cref="MockDistributionServiceSubscriber"/> instance.
        /// </param>
        /// <param name="fragmentStorageProvider">
        /// The storage provider where the fragments are stored to and retrived from.
        /// </param>
        /// <param name="options">
        /// The options to configure this <see cref="MockDistributionServiceSubscriber"/> instance.
        /// </param>
        public MockDistributionServiceSubscriber(
            ILogger<MockDistributionServiceSubscriber> logger,
            IFragmentStorageProvider fragmentStorageProvider,
            IOptions<MockDistributionServiceSubscriberOptions>? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fragmentStorageProvider = fragmentStorageProvider ?? throw new ArgumentNullException(nameof(fragmentStorageProvider));

            _fragmentSize = 1024;

            var sha256 = HashAlgorithm.Create("SHA256");
            var inc = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            using (FileStream fs = new FileStream("C:\\Users\\Dominik.LIN-NET\\Downloads\\000.png", FileMode.Open, FileAccess.Read))
            {
                fileSize = fs.Length;

                long length = fs.Length;

                byte[] hash;
                string hashString;

                while (length > 0)
                {
                    byte[] fragment = new byte[length >= _fragmentSize.Value ? _fragmentSize.Value : length];

                    length -= fs.Read(fragment);

                    inc.AppendData(fragment);

                    hash = sha256.ComputeHash(fragment);
                    hashString = "";

                    foreach (byte b in hash)
                    {
                        hashString += b.ToString("X2");
                    }

                    _fragmentSequence.Add(hashString);
                    fragmentStorageProvider.StoreFragmentAsync(hashString, fragment).Wait();
                }

                hash = inc.GetHashAndReset();
                hashString = "";

                foreach (byte b in hash)
                {
                    hashString += b.ToString("X2");
                }

                fileHash = hashString;
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MockDistributionServiceSubscriber" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~MockDistributionServiceSubscriber()
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
        /// This method was called after the <see cref="MockDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public long FragmentSize
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();

                if (_fragmentSize.HasValue)
                    return _fragmentSize.Value;

                throw new InvalidOperationException("Information not available yet.");
            }
        }

        /// <summary>
        /// Gets the uris of the endpoints where the fragments are distributed to an d can be retrived from.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MockDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public IReadOnlyCollection<Uri> DistributionEndPoints
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return new Uri[] { new Uri("grpc://127.0.0.1:50051") };
            }
        }

        /// <summary>
        /// Gets the <see cref="IFragmentStorageProvider"/> where the fragments are stored which this <see cref="IDistributionServiceSubscriber"/> decided to hold.
        /// </summary>
        /// <remarks>
        /// An <see cref="IDistributionServiceSubscriber"/> of course don't have to hold any fragment that are published by the <see cref="IDistributionServicePublisher"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MockDistributionServiceSubscriber"/> was disposed.
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
        /// This method was called after the <see cref="MockDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool ExistsFile(string fileHash)
        {
            EnsureStorageProviderWasNotDisposed();

            return fileHash.ToUpper().Equals(this.fileHash);
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
        /// This method was called after the <see cref="MockDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool ExistsFragment(string fragmentHash)
        {
            EnsureStorageProviderWasNotDisposed();

            return _fragmentSequence.Contains(fragmentHash);
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
        /// This method was called after the <see cref="MockDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool TryGetFileInfo(string fileHash, [NotNullWhen(true)] out IFragmentedFileInfo? fileInfo)
        {
            EnsureStorageProviderWasNotDisposed();

            if (ExistsFile(fileHash))
            {
                fileInfo = new FFI()
                {
                    Hash = this.fileHash,
                    Size = this.fileSize,
                    FragmentSequence = _fragmentSequence
                };

                return true;
            }

            fileInfo = null;
            return false;
        }

        class FFI : IFragmentedFileInfo
        {
            public string Hash { get; set; } = "";

            public long Size { get; set; } = 0;

            public IEnumerable<string> FragmentSequence { get; set; } = Array.Empty<string>();
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
        /// This method was called after the <see cref="MockDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool TryGetFragmentDistribution(string fragmentHash, out IEnumerable<Uri> fragmentUris)
        {
            EnsureStorageProviderWasNotDisposed();

            if (ExistsFragment(fragmentHash))
            {
                fragmentUris = new Uri[] { new Uri("grpc://127.0.0.1:50051/" + fragmentHash) };
                return true;
            }

            fragmentUris = Array.Empty<Uri>();
            return false;
        }

        /// <summary>
        /// Releases all allocated resources associated to this <see cref="MockDistributionServiceSubscriber" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MockDistributionServiceSubscriber" /> and
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
                //release managed resources here
            }
        }
    }
}

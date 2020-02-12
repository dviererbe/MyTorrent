using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    partial class VirtualManagedFragmentFileStorageProvider
    {
        /// <summary>
        /// TODO: DOCUMENT "internal class VirtualManagedFragmentFileReadStream : FileStream"
        /// </summary>
        internal class VirtualManagedFragmentFileReadStream : FileStream
        {
            private volatile bool _disposed = false;
            private readonly VirtualManagedFragmentFileStorageProvider _storageProvider;

            /// <summary>
            /// TODO: DOCUMENT "private VirtualManagedFragmentFileReadStream(string normalizedFragmentHash,string fragmentFilePath,VirtualManagedFragmentFileStorageProvider storageProvider)"
            /// </summary>
            /// <param name="normalizedFragmentHash">
            ///
            /// </param>
            /// <param name="fragmentFilePath">
            ///
            /// </param>
            /// <param name="storageProvider">
            ///
            /// </param>
            private VirtualManagedFragmentFileReadStream(
                string normalizedFragmentHash,
                string fragmentFilePath,
                VirtualManagedFragmentFileStorageProvider storageProvider)
                : base(fragmentFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
            {
                _storageProvider = storageProvider;
                FragmentHash = normalizedFragmentHash;
            }

            /// <summary>
            /// Frees resources before it is reclaimed by garbage collection.
            /// </summary>
            ~VirtualManagedFragmentFileReadStream()
            {
                Dispose(false);
            }

            /// <summary>
            /// TODO: DOCUMENT "public static VirtualManagedFragmentFileReadStream Create(string fragmentHash, bool delete, VirtualManagedFragmentFileStorageProvider storageProvider)"
            /// </summary>
            /// <param name="fragmentHash">
            ///
            /// </param>
            /// <param name="delete">
            ///
            /// </param>
            /// <param name="storageProvider">
            ///
            /// </param>
            /// <returns>
            ///
            /// </returns>
            public static VirtualManagedFragmentFileReadStream Create(
                string fragmentHash,
                bool delete,
                VirtualManagedFragmentFileStorageProvider storageProvider)
            {
                if (fragmentHash == null)
                    throw new ArgumentNullException(nameof(fragmentHash));

                if (!storageProvider._hashingServiceProvider.Validate(fragmentHash))
                    throw new FormatException("Invalid fragment hash format.");

                fragmentHash = storageProvider._hashingServiceProvider.Normalize(fragmentHash);

                try
                {
                    storageProvider._lock.Wait();

                    if (storageProvider._fragments.TryGetValue(fragmentHash, out FragmentMetadata fragmentMetadata))
                    {
                        string fragmentFilePath = fragmentMetadata.Persistent 
                            ? storageProvider.GetCommittedFragmentPath(fragmentMetadata.NormalizedFragmentHash) 
                            : storageProvider.GetNonCommittedFragmentPath(fragmentMetadata.NormalizedFragmentHash);

                        try
                        {
                            var readStream = new VirtualManagedFragmentFileReadStream(
                                fragmentMetadata.NormalizedFragmentHash,
                                fragmentFilePath,
                                storageProvider);

                            fragmentMetadata.ReadOperations.Add(readStream);

                            if (delete)
                                fragmentMetadata.Remove = true;

                            return readStream;
                        }
                        catch (Exception exception)
                        {
                            throw new IOException("Failed to open fragment file.", exception);
                        }
                    }
                }
                finally
                {
                    storageProvider._lock.Release();
                }

                throw new KeyNotFoundException($"No fragment with the specified hash '{fragmentHash}' was found.");
            }

            /// <summary>
            /// Gets if the <see cref="VirtualManagedFragmentFileReadStream"/> was disposed.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the <see cref="VirtualManagedFragmentFileReadStream"/> instance was disposed; otherwise <see langword="false"/>.
            /// </returns>
            public bool Disposed => _disposed;

            /// <summary>
            /// Gets the normalizes fragment hash value of the fragment that is read from.
            /// </summary>
            public string FragmentHash { get; }

            /// <summary>
            /// Releases the unmanaged resources used by the <see cref="VirtualManagedFragmentFileReadStream" /> and
            /// optionally releases the managed resources.
            /// </summary>
            /// <param name="disposing">
            /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
            /// release only unmanaged resources.
            /// </param>
            protected override void Dispose(bool disposing)
            {
                //redundant, but will prevent to call "_lock.Wait()" and temporary blocking
                if (_disposed)
                    return;

                _disposed = true;

                base.Dispose(disposing);

                try
                {
                    _storageProvider._lock.Wait();

                    var fragmentMetadata = _storageProvider._fragments[FragmentHash];
                    fragmentMetadata.ReadOperations.Remove(this);

                    if (fragmentMetadata.Remove && fragmentMetadata.ReadOperations.Count == 0)
                    {
                        _storageProvider.DeleteFragmentAsyncCore(FragmentHash, wait: true).GetAwaiter().GetResult();
                    }
                }
                catch
                {
                    //Do Nothing... Dispose throws no exceptions
                    //TODO: IMPLEMENT log warning
                }
                finally
                {
                    _storageProvider._lock.Release();
                }
            }
        }
    }
}

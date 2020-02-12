using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    partial class VirtualManagedFragmentFileStorageProvider
    {
        /// <summary>
        /// Stream which reads a fragment that is stored on the filesystem.
        /// </summary>
        internal class VirtualManagedFragmentFileReadStream : FileStream
        {
            private volatile bool _disposed = false;
            private readonly VirtualManagedFragmentFileStorageProvider _storageProvider;

            /// <summary>
            /// Initializes a new <see cref="VirtualManagedFragmentFileReadStream"/> instance.
            /// </summary>
            /// <param name="normalizedFragmentHash">
            /// Normalized fragment hash value of the fragment that should be read.
            /// </param>
            /// <param name="fragmentFilePath">
            /// File path where the fragment content is stored.
            /// </param>
            /// <param name="storageProvider">
            /// Storage provider where the fragment is stored.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="fragmentFilePath"/> is <see langword="null"/>.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="fragmentFilePath"/> is an empty string (""), contains only white space, or contains one or more
            /// invalid characters. -or- <paramref name="fragmentFilePath"/> refers to a non-file device, such as "con:", "com1:",
            /// "lpt1:", etc. in an NTFS environment.
            /// </exception>
            /// <exception cref="NotSupportedException">
            /// path refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.
            /// </exception>
            /// <exception cref="FileNotFoundException">
            /// The file could not be found.
            /// </exception>
            /// <exception cref="IOException">
            /// An I/O error, such as the stream has been closed.
            /// </exception>
            /// <exception cref="SecurityException">
            /// The caller does not have the required permission.
            /// </exception>
            /// <exception cref="DirectoryNotFoundException">
            /// The specified <paramref name="fragmentFilePath"/> is invalid, such as being on an unmapped drive.
            /// </exception>
            /// <exception cref="UnauthorizedAccessException">
            /// The access requested is not permitted by the operating system for the specified <paramref name="fragmentFilePath"/>.
            /// </exception>
            /// <exception cref="PathTooLongException">
            /// The specified path, file name, or both exceed the system-defined maximum length.
            /// </exception>
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
            /// Creates a new <see cref="VirtualManagedFragmentFileReadStream"/> to read the content 
            /// of a fragment with a specific hash value from.
            /// </summary>
            /// <param name="fragmentHash">
            /// The hash value of the fragment to read the fragemnt from.
            /// </param>
            /// <param name="delete">
            /// <see langword="true"/> if the fragment should be deleted when the last 
            /// <see cref="VirtualManagedFragmentFileReadStream"/> is closed that reads the 
            /// specified fragement; otherwise <see langword="false"/>.
            /// </param>
            /// <param name="storageProvider">
            /// Storage provider where the fragment is stored.
            /// </param>
            /// <returns>
            /// The <see cref="VirtualManagedFragmentFileReadStream"/> to read the content of the specified fragment from.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="fragmentHash"/> is <see langword="null"/>.
            /// </exception>
            /// <exception cref="FormatException">
            /// <paramref name="fragmentHash"/> contains an invalid hash value.
            /// </exception>
            /// <exception cref="KeyNotFoundException">
            /// No fragment with the specified <paramref name="fragmentHash"/> was found.
            /// </exception>
            /// <exception cref="IOException">
            /// Failed to open the filestream.
            /// </exception>
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

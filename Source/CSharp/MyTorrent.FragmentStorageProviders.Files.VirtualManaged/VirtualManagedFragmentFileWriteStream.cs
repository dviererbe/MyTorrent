using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyTorrent.FragmentStorageProviders
{
    partial class VirtualManagedFragmentFileStorageProvider
    {
        /// <summary>
        /// Stream which writes a fragment that is stored on the filesystem.
        /// </summary>
        internal class VirtualManagedFragmentFileWriteStream : FileStream
        {
            private volatile bool _disposed = false;
            private readonly VirtualManagedFragmentFileStorageProvider _storageProvider;
            private readonly VirtualManagedFileStorageSpaceAllocationToken? _allocationToken;

            /// <summary>
            /// Initializes a new <see cref="VirtualManagedFragmentFileWriteStream"/> instance.
            /// </summary>
            /// <param name="normalizedFragmentHash">
            /// Normalizes fragment hash value of the fragment that should be written.
            /// </param>
            /// <param name="fragmentFilePath">
            /// The file path where the content of the fragment should be temporarily stored.
            /// </param>
            /// <param name="length">
            /// Size of the fragment content in bytes.
            /// </param>
            /// <param name="storageProvider">
            /// Storage provider where the fragment should be stored.
            /// </param>
            /// <param name="allocationToken">
            /// If not <see langword="null"/> the resources of the allocated resources associated to 
            /// this <paramref name="allocationToken"/> will be used to store the fragment.
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
            /// <exception cref="IOException">
            /// An I/O error, such as a file with the specified <paramref name="fragmentFilePath"/> already exists.
            /// </exception>
            /// <exception cref="SecurityException">
            /// The caller does not have the required permission.
            /// </exception>
            /// <exception cref="DirectoryNotFoundException">
            /// The specified <paramref name="fragmentFilePath"/> is invalid, such as being on an unmapped drive.
            /// </exception>
            /// <exception cref="UnauthorizedAccessException">
            /// The access requested is not permitted by the operating system for the specified
            /// path, such as when access is Write or ReadWrite and the file or directory is
            /// set for read-only access.
            /// </exception>
            /// <exception cref="PathTooLongException">
            /// The specified path, file name, or both exceed the system-defined maximum length.
            /// </exception>
            private VirtualManagedFragmentFileWriteStream(
                string normalizedFragmentHash,
                string fragmentFilePath,
                long length,
                VirtualManagedFragmentFileStorageProvider storageProvider,
                VirtualManagedFileStorageSpaceAllocationToken? allocationToken = null)
                : base(fragmentFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)
            {
                FragmentHash = normalizedFragmentHash;
                _allocationToken = allocationToken;
                _storageProvider = storageProvider;

                base.SetLength(length);
            }

            /// <summary>
            /// Frees resources before it is reclaimed by garbage collection.
            /// </summary>
            ~VirtualManagedFragmentFileWriteStream()
            {
                Dispose(false);
            }

            /// <summary>
            /// Creates a <see cref="VirtualManagedFragmentFileWriteStream"/> instance to write the content.
            /// </summary>
            /// <param name="fragmentHash">
            /// Normalizes fragment hash value of the fragment that should be written.
            /// </param>
            /// <param name="fragmentSize">
            /// Size of the fragment content in bytes.
            /// </param>
            /// <param name="storageProvider">
            /// Storage provider where the fragment should be stored.
            /// </param>
            /// <param name="allocationToken">
            /// If not <see langword="null"/> the resources of the allocated resources associated to 
            /// this <paramref name="allocationToken"/> will be used to store the fragment.
            /// </param>
            /// <returns>
            /// The <see cref="VirtualManagedFragmentFileWriteStream"/> to write the content of the fragment to.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="fragmentHash"/> is <see langword="null"/>.
            /// </exception>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="fragmentSize"/> is negative.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// An fragment with the specified <paramref name="fragmentHash"/> already exists.
            /// -or- <paramref name="allocationToken"/> is not <see langword="null"/> and unknown to the <paramref name="storageProvider"/>.
            /// </exception>
            /// <exception cref="FormatException">
            /// <paramref name="fragmentHash"/> contains an invalid hash value.
            /// </exception>
            /// <exception cref="IOException">
            /// An other write operation already tries to write a fragment with the same hash value.
            /// -or- Failed to open the filestream.
            /// </exception>
            /// <exception cref="StorageSpaceAllocationException">
            /// Less storage space is available than <paramref name="fragmentSize"/> specifies as needed.
            /// </exception>
            /// <exception cref="ObjectDisposedException">
            /// Allocation token is not <see langword="null"/> and was disposed.
            /// </exception>
            public static VirtualManagedFragmentFileWriteStream Create(
                string fragmentHash, 
                long fragmentSize, 
                VirtualManagedFragmentFileStorageProvider storageProvider, 
                IStorageSpaceAllocationToken? allocationToken = null)
            {
                if (fragmentHash == null)
                    throw new ArgumentNullException(nameof(fragmentHash));

                if (fragmentSize < 0)
                    throw new ArgumentOutOfRangeException(nameof(fragmentSize), fragmentSize, "Fragment size can't be negative.");

                if (!storageProvider._hashingServiceProvider.Validate(fragmentHash))
                    throw new FormatException("Invalid fragment hash format.");

                fragmentHash = storageProvider._hashingServiceProvider.Normalize(fragmentHash);

                try
                {
                    storageProvider._lock.Wait();

                    if (storageProvider._fragments.ContainsKey(fragmentHash))
                        throw new ArgumentException("An fragment with the same hash value already exists", nameof(fragmentHash));

                    if (storageProvider._writeOperations.ContainsKey(fragmentHash))
                        throw new IOException("Unable to write fragment. An write operation is ongoing.");

                    storageProvider.AllocateStorageSpace(fragmentSize, allocationToken);

                    var token = allocationToken as VirtualManagedFileStorageSpaceAllocationToken;

                    VirtualManagedFragmentFileWriteStream writeStream;

                    try
                    {
                        writeStream = new VirtualManagedFragmentFileWriteStream(
                            fragmentHash, 
                            storageProvider.GetTempFragmentPath(fragmentHash), 
                            fragmentSize, 
                            storageProvider, 
                            token);
                    }
                    catch (Exception exception)
                    {
                        storageProvider.DeallocateStorageSpace(fragmentSize, token);
                        throw new IOException("Failed to open file stream.", exception);
                    }

                    storageProvider._writeOperations.Add(fragmentHash, writeStream);
                    token?._unwrittenFragments.Add(fragmentHash);
                    return writeStream;
                }
                finally
                {
                    storageProvider._lock.Release();
                }
            }

            /// <summary>
            /// Gets if the <see cref="VirtualManagedFragmentFileWriteStream"/> was disposed.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the <see cref="VirtualManagedFragmentFileWriteStream"/> instance was disposed; otherwise <see langword="false"/>.
            /// </returns>
            public bool Disposed => _disposed;

            /// <summary>
            /// Gets the normalizes fragment hash value of the fragment that is written to.
            /// </summary>
            public string FragmentHash { get; }

            /// <summary>
            /// Sets the length of this stream to the given value.
            /// </summary>
            /// <param name="value">
            /// The new length of the stream.
            /// </param>
            /// <exception cref="NotSupportedException">
            /// The stream does not support both writing and seeking.
            /// </exception>
            [DoesNotReturn]
            public override void SetLength(long value)
            { 
                throw new NotSupportedException("Length of a fragment can't be changed.");
            }

            /// <summary>
            /// Releases the unmanaged resources used by the <see cref="VirtualManagedFragmentFileWriteStream" /> and
            /// optionally releases the managed resources.
            /// </summary>
            /// <param name="disposing">
            /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
            /// </param>
            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                    return;
                
                _disposed = true;

                long length = Length;

                base.Dispose(disposing);

                try
                {
                    _storageProvider._lock.Wait();

                    bool error = false;
                    FileInfo file = new FileInfo(_storageProvider.GetTempFragmentPath(FragmentHash));

                    if (_allocationToken == null || !_allocationToken.Disposed)
                    {
                        try
                        {
                            file.MoveTo(_allocationToken == null
                                ? _storageProvider.GetCommittedFragmentPath(FragmentHash)
                                : _storageProvider.GetNonCommittedFragmentPath(FragmentHash));

                            _storageProvider._fragments.Add(FragmentHash, new FragmentMetadata(FragmentHash, length, _allocationToken == null, _allocationToken));
                            _allocationToken?._fragments.Add(FragmentHash);
                        }
                        catch (Exception exception)
                        {
                            error = true;
                            _storageProvider._logger.LogCritical($"Failed to move/rename an temporary fragment file (Fragment Hash: {FragmentHash}; Path: {_storageProvider.GetTempFragmentPath(FragmentHash)})! This can lead to critical data loss or an unrecoverable application state.", exception);
                        }
                    }

                    if (error || (_allocationToken != null && _allocationToken.Disposed))
                    {
                        try
                        {
                            File.Delete(_storageProvider.GetTempFragmentPath(FragmentHash));
                        }
                        catch (Exception exception)
                        {
                            _storageProvider._logger.LogCritical($"Failed to delete an temporary fragment file (Fragment Hash: {FragmentHash}; Path: {_storageProvider.GetTempFragmentPath(FragmentHash)})! This can lead to critical data loss or an unrecoverable application state.", exception);
                        }
                    }

                    _storageProvider._writeOperations.Remove(FragmentHash);
                    _allocationToken?._unwrittenFragments.Remove(FragmentHash);

                    //Storagespace deallocation???
                }
                finally
                {
                    _storageProvider._lock.Release();
                }   
            }
        }
    }
}

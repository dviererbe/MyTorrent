using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyTorrent.FragmentStorageProviders
{
    partial class VirtualManagedFragmentFileStorageProvider
    {
        /// <summary>
        /// TODO: DOCUMENT "internal class VirtualManagedFragmentFileWriteStream"
        /// </summary>
        internal class VirtualManagedFragmentFileWriteStream : FileStream
        {
            private volatile bool _disposed = false;
            private readonly VirtualManagedFragmentFileStorageProvider _storageProvider;
            private readonly VirtualManagedFileStorageSpaceAllocationToken? _allocationToken;

            /// <summary>
            /// TODO: DOCUMENT "private VirtualManagedFragmentFileWriteStream(string normalizedFragmentHash, string fragmentFilePath, long length, VirtualManagedFragmentFileStorageProvider storageProvider, VirtualManagedFileStorageSpaceAllocationToken? allocationToken = null)"
            /// </summary>
            /// <param name="normalizedFragmentHash">
            ///
            /// </param>
            /// <param name="fragmentFilePath">
            ///
            /// </param>
            /// <param name="length">
            ///
            /// </param>
            /// <param name="storageProvider">
            ///
            /// </param>
            /// <param name="allocationToken">
            ///
            /// </param>
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
            /// TODO: DOCUMENT "public static VirtualManagedFragmentFileWriteStream Create(string fragmentHash, long fragmentSize, VirtualManagedFragmentFileStorageProvider storageProvider, IStorageSpaceAllocationToken? allocationToken = null)"
            /// </summary>
            /// <param name="fragmentHash">
            ///
            /// </param>
            /// <param name="fragmentSize">
            ///
            /// </param>
            /// <param name="storageProvider">
            ///
            /// </param>
            /// <param name="allocationToken">
            ///
            /// </param>
            /// <returns>
            ///
            /// </returns>
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
                    catch
                    {
                        storageProvider.DeallocateStorageSpace(fragmentSize, token);
                        throw;
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

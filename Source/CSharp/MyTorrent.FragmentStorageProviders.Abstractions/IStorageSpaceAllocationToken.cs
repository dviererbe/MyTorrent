using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Token that is associated to an previously allocated part of storage space to that fragments can associated to.
    /// </summary>
    /// <remarks>
    /// This can be used if you know e.g. you have to store 5 fragments with 5000 bytes total.
    /// So you can allocate 5000 bytes and associate the allocation token to the fragments when storing them.
    /// This grantees you the storage space.
    /// 
    /// Disposal of an <see cref="IStorageSpaceAllocationToken"/> will result in removal of all associated fragments. 
    /// </remarks>
    public interface IStorageSpaceAllocationToken : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// <see langword="true"/> if the allocated resources associated to this token were released ;otherwise <see langword="false"/>.
        /// </summary>
        bool Disposed { get; }

        /// <summary>
        /// Gets how many bytes are left unused of the allocated storage space, associated to this token.
        /// </summary>
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the allocation token was disposed.
        /// </exception>
        long AvailableFreeSpace { get; }

        /// <summary>
        /// Gets how many bytes were already used of the allocated storage space, associated to this token.
        /// </summary>
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the allocation token was disposed.
        /// </exception>
        long UsedSpace { get; }

        /// <summary>
        /// Gets how many bytes were allocated in total which are associated to this token.
        /// </summary>
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
        long TotalAllocatedStorageSpace { get; }

        /// <summary>
        /// Gets a list with hash values of all fragments which are stored in the allocated storage space associated to this token.
        /// </summary>
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the allocation token was disposed.
        /// </exception>
        IEnumerable<string> Fragments { get; }

        /// <summary>
        /// Gets a list with hash values of all fragments which are stored in the allocated storage space associated to this token and are persistent.
        /// They will be accessible even after the allocation token is disposed.
        /// </summary>
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the allocation token was disposed.
        /// </exception>
        IEnumerable<string> PersistentFragments { get; }

        /// <summary>
        /// Asynchronously Makes the to this associated allocated resources persistent. Even after the allocation token was disposed the fragments will be accessible.
        /// </summary>
        /// <remarks>
        /// This Operation tries to be atomic. It succeeds completely or fails completely.
        /// </remarks>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// </returns>
        /// <exception cref="IOException">
        /// Operation to commit non-persistent fragments to the underlying storage system failed.
        /// </exception>
        /// <exception cref="AggregateException">
        /// Operation to commit non-persistent fragments to the underlying storage system failed and
        /// also failed to revert one or more operation(s) to revert successfully committed fragments.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Methods were called after the allocation token was disposed.
        /// </exception>
        Task CommitAsync();
    }
}

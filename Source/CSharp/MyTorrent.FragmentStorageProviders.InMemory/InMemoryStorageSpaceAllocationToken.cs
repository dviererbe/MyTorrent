using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyTorrent.FragmentStorageProviders
{
    public partial class FragmentInMemoryStorageProvider
    {
        /// <summary>
        /// Token that is associated to an previously allocated part of in-memory storage space to that fragments can associated to.
        /// </summary>
        /// <remarks>
        /// This can be used if you know e.g. you have to store 5 fragments with 5000 bytes total.
        /// So you can allocate 5000 bytes and associate the allocation token to the fragments when storing them.
        /// This grantees you the storage space.
        /// 
        /// Disposal of an <see cref="InMemoryStorageSpaceAllocationToken"/> will result in removal of all associated fragments. 
        /// </remarks>
        internal class InMemoryStorageSpaceAllocationToken : IStorageSpaceAllocationToken
        {
            /// <summary>
            /// Unique Id of the <see cref="InMemoryStorageSpaceAllocationToken"/>.
            /// </summary>
            /// <remarks>
            /// This is used to monitor allocations.
            /// </remarks>
            public readonly Guid ID = Guid.NewGuid();

            private volatile bool _disposing = false;
            private volatile bool _disposed = false;

            internal long _usedStorageSpace;
            internal List<string> _fragments;
            internal List<string> _unwrittenFragments;
            private readonly FragmentInMemoryStorageProvider _storageProvider;

            /// <summary>
            /// Initializes a new <see cref="InMemoryStorageSpaceAllocationToken"/> instance.
            /// </summary>
            /// <param name="size">
            /// How many bytes were allocated.
            /// </param>
            /// <param name="fragmentInMemoryStorageProvider">
            /// The storage provider whose storage space was allocated.
            /// </param>
            private InMemoryStorageSpaceAllocationToken(long size, FragmentInMemoryStorageProvider fragmentInMemoryStorageProvider)
            {
                _usedStorageSpace = 0;
                TotalAllocatedStorageSpace = size;

                _storageProvider = fragmentInMemoryStorageProvider;
                _fragments = new List<string>();
                _unwrittenFragments = new List<string>();
            }

            /// <summary>
            /// Frees resources before it is reclaimed by garbage collection.
            /// </summary>
            ~InMemoryStorageSpaceAllocationToken()
            {
                Dispose(false);
            }

            /// <summary>
            /// Asynchronously allocates a specific amount of storage space and returns an token that represents the allocation and by which further allocations
            /// for the allocated storage space can be made.
            /// </summary>
            /// <param name="size">
            /// How many bytes of the storage space should be allocated.
            /// </param>
            /// <param name="storageProvider">
            /// The storage provider from whom storage space should be allocated.
            /// </param>
            /// <returns>
            /// An <see cref="InMemoryStorageSpaceAllocationToken"/> instance that represents the allocation made and by which further allocations
            /// for the allocated storage space can be made.
            /// </returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="size"/> is negative.
            /// </exception>
            /// <exception cref="StorageSpaceAllocationException">
            /// Less storage space is available than <paramref name="size"/> specifies as needed.
            /// </exception>
            public static async Task<InMemoryStorageSpaceAllocationToken> CreateAsync(
                long size,
                FragmentInMemoryStorageProvider storageProvider)
            {
                InMemoryStorageSpaceAllocationToken allocationToken;

                try
                {
                    await storageProvider._lock.WaitAsync();

                    storageProvider.AllocateStorageSpace(size);

                    allocationToken = new InMemoryStorageSpaceAllocationToken(size, storageProvider);
                    storageProvider._allocations.Add(allocationToken);
                }
                finally
                {
                    storageProvider._lock.Release();
                }
#if DEBUG
                storageProvider._logger.LogDebug($"Allocated {size} bytes of storage space. (Allocation Token ID: {allocationToken.ID})");
#endif
                return allocationToken;
            }

            /// <summary>
            /// <see langword="true"/> if the allocated resources associated to this token were released ;otherwise <see langword="false"/>.
            /// </summary>
            public bool Disposed => _disposed; 

            /// <summary>
            /// Gets how many bytes are left unused of the allocated storage space, associated to this token.
            /// </summary>
            /// <exception cref="ObjectDisposedException">
            /// Methods were called after the allocation token was disposed.
            /// </exception>
            public long AvailableFreeSpace
            {
                get
                {
                    EnsureTokenWasNotDisposed();

                    long usedStorageSpace = Interlocked.Read(ref _usedStorageSpace);
                    return usedStorageSpace < TotalAllocatedStorageSpace ? TotalAllocatedStorageSpace - usedStorageSpace : 0L;
                }
            }

            /// <summary>
            /// Gets how many bytes were already used of the allocated storage space, associated to this token.
            /// </summary>
            /// <exception cref="ObjectDisposedException">
            /// Methods were called after the allocation token was disposed.
            /// </exception>
            public long UsedSpace
            {
                get
                {
                    EnsureTokenWasNotDisposed();
                    return Interlocked.Read(ref _usedStorageSpace);
                }
            }

            /// <summary>
            /// Gets how many bytes were already used of the allocated storage space, associated to this token.
            /// </summary>
            /// <exception cref="IOException">
            /// The operation to the underlying storage system failed.
            /// </exception>
            public long TotalAllocatedStorageSpace { get; }

            /// <summary>
            /// Gets a list with hash values of all fragments which are stored in the allocated storage space associated to this token.
            /// </summary>
            public IEnumerable<string> Fragments
            {
                get
                {
                    EnsureTokenWasNotDisposed();
                    return _fragments;
                }
            }

            /// <summary>
            /// Gets a list with hash values of all fragments which are stored in the allocated storage space associated to this token and are persistent.
            /// They will be accessible even after the allocation token is disposed.
            /// </summary>
            /// <exception cref="ObjectDisposedException">
            /// Methods were called after the allocation token was disposed.
            /// </exception>
            public IEnumerable<string> PersistentFragments
            {
                get
                {
                    EnsureTokenWasNotDisposed();
                    return _fragments.Where(fragmentHash => _storageProvider._fragments[fragmentHash].Persistent);
                }
            }

            /// <summary>
            /// Ensures that the allocation token was not disposed.
            /// </summary>
            /// <exception cref="ObjectDisposedException">
            /// Method were called after this allocation token was disposed.
            /// </exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void EnsureTokenWasNotDisposed()
            {
                if (_disposing)
                    throw new ObjectDisposedException(ToString(), "Allocation token is disposing or was already disposed.");
            }

            /// <summary>
            /// Asynchronously Makes the to this associated allocated resources persistent. Even after the allocation token was disposed the fragments will be accessible.
            /// </summary>
            /// <returns>
            /// A task that represents the asynchronous write operation.
            /// </returns>
            /// <exception cref="IOException">
            /// The operation to underlying storage system failed.
            /// </exception>
            /// <exception cref="ObjectDisposedException">
            /// Method were called after this storage provider was disposed.
            /// </exception>
            public async Task CommitAsync()
            {
                EnsureTokenWasNotDisposed();

                try
                {
                    await _storageProvider._lock.WaitAsync();

                    foreach (string fragmentHash in _fragments)
                    {
                        _storageProvider._fragments[fragmentHash].Persistent = true;
                    }
                }
                finally
                {
                    _storageProvider._lock.Release();
                }
            }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>
            /// A string that represents the current object.
            /// </returns>
            public override string ToString()
            {
                return GetType().FullName + " (" + ID + ")";
            }

            /// <summary>
            /// Releases all allocated resources associated to the <see cref="InMemoryStorageSpaceAllocationToken" />.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Asynchronously releases the resources used by the <see cref="InMemoryStorageSpaceAllocationToken" />.
            /// </summary>
            /// <returns>
            /// A task that represents the asynchronous dispose operation.
            /// </returns>
            public async ValueTask DisposeAsync()
            {
                if (_disposing)
                    return;

                _disposing = true;

                try
                {
                    await _storageProvider._lock.WaitAsync();

                    foreach (string fragmentHash in _unwrittenFragments)
                    {
                        var writeStream = _storageProvider._writeOperations[fragmentHash];
                        await writeStream.DisposeAsync();
                    }

                    long persistentStorage = 0L;

                    //copy the collection because the _fragments collection is manipulated while iterating over it.
                    string[] fragments = _fragments.ToArray();

                    foreach (string fragmentHash in fragments)
                    {
                        var fragmentMetadata = _storageProvider._fragments[fragmentHash];
                        
                        if (fragmentMetadata.Persistent)
                        {
                            fragmentMetadata.AllocationToken = null;
                            persistentStorage += fragmentMetadata.Data.LongLength;
                            continue;
                        }

                        foreach (var readOperation in fragmentMetadata.ReadOperations)
                        {
                            await readOperation.DisposeAsync();
                        }

                        await _storageProvider.DeleteFragmentAsyncCore(fragmentHash, wait: true);

                        //should already be empty, but just to get sure...
                        fragmentMetadata.ReadOperations.Clear();
                    }

                    _storageProvider._allocations.Remove(this);
                    _storageProvider.DeallocateStorageSpace(TotalAllocatedStorageSpace - persistentStorage);
                }
                finally
                {
                    _storageProvider._lock.Release();
                    _unwrittenFragments.Clear();
                    _fragments.Clear();

                    _disposed = true;
                }
            }

            /// <summary>
            /// Releases the unmanaged resources used by the <see cref="InMemoryStorageSpaceAllocationToken" /> and
            /// optionally releases the managed resources.
            /// </summary>
            /// <param name="disposing">
            /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
            /// release only unmanaged resources.
            /// </param>
            protected virtual void Dispose(bool disposing)
            {
                if (_disposing)
                    return;

                _disposing = true;

                try
                {
                    _storageProvider._lock.Wait();

                    foreach (string fragmentHash in _unwrittenFragments)
                    {
                        var writeStream = _storageProvider._writeOperations[fragmentHash];
                        writeStream.Dispose();
                    }

                    long persistentStorage = 0L;

                    //copy the collection because the _fragments collection is manipulated while iterating over it.
                    string[] fragments = _fragments.ToArray();

                    foreach (string fragmentHash in fragments)
                    {
                        var fragmentMetadata = _storageProvider._fragments[fragmentHash];

                        if (fragmentMetadata.Persistent)
                        {
                            fragmentMetadata.AllocationToken = null;
                            persistentStorage += fragmentMetadata.Data.LongLength;
                            continue;
                        }

                        foreach (var readOperation in fragmentMetadata.ReadOperations)
                        {
                            readOperation.Dispose();
                        }

                        _storageProvider.DeleteFragmentAsyncCore(fragmentHash, wait: true).GetAwaiter().GetResult();

                        //should already be empty, but just to get sure...
                        fragmentMetadata.ReadOperations.Clear();
                    }

                    _storageProvider._allocations.Remove(this);
                    _storageProvider.DeallocateStorageSpace(TotalAllocatedStorageSpace - persistentStorage);
                }
                finally
                {
                    _storageProvider._lock.Release();
                    _unwrittenFragments.Clear();
                    _fragments.Clear();

                    _disposed = true;
                }
            }
        }
    }
}

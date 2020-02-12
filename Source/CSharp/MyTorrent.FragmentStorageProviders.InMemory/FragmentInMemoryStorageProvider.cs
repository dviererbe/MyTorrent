using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MyTorrent.HashingServiceProviders;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// An <see cref="IFragmentStorageProvider"/> that stores the fragment data in the working memory.
    /// </summary>
    /// <remarks>
    /// WARNING: The fragment data is stored in the working memory of the computer and is therefore non-persistent.
    ///          After the application and/or computer restarts and/or crashes; all data is gone.
    /// </remarks>
    public sealed partial class FragmentInMemoryStorageProvider : IFragmentStorageProvider
    {
        #region Private Variables

        //TODO: IMPLEMENT Log Messages
        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<FragmentInMemoryStorageProvider> _logger;

        private readonly IHashingServiceProvider _hashingServiceProvider;

        /// <summary>
        /// Locking mechanism to ensure thread safety.
        /// </summary>
        private readonly SemaphoreSlim _lock;

        /// <summary>
        /// List that contains all non-disposed allocations.
        /// </summary>
        private readonly List<InMemoryStorageSpaceAllocationToken> _allocations;

        /// <summary>
        /// List with the hash values of all fragments which are currently written and therefore can not be read from or deleted.
        /// </summary>
        private readonly Dictionary<string, FragmentInMemoryWriteStream> _writeOperations;

        /// <summary>
        /// Dictionary with the hash values of all fragments and their byte data.
        /// </summary>
        private readonly Dictionary<string, InMemoryFragment> _fragments;

        /// <summary>
        /// How many bytes of the storage space were already allocated.
        /// </summary>
        private long _usedStorageSpace;

        /// <summary>
        /// The Limit how many bytes can be allocated.
        /// </summary>
        private readonly long _storageSpaceUsageLimit;

        /// <summary>
        /// <see langword="true"/> if the <see cref="FragmentInMemoryStorageProvider"/> instance was disposed; otherwise <see langword="false"/>.
        /// </summary>
        private volatile bool _disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="FragmentInMemoryStorageProvider"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger instance for this <see cref="FragmentInMemoryStorageProvider"/> instance.
        /// </param>
        /// <param name="eventIdCreationSource">
        /// The <see cref="IEventIdCreationSource"/> instance for this <see cref="FragmentInMemoryStorageProvider"/> instance
        /// to create <see cref="EventId"/> instances for every log event.
        /// </param>
        /// <param name="hashingServiceProvider">
        /// The 
        /// </param>
        /// <param name="options">
        /// Configuration options for this <see cref="FragmentInMemoryStorageProvider"/>.
        /// </param>
        public FragmentInMemoryStorageProvider(
            ILogger<FragmentInMemoryStorageProvider> logger, 
            IEventIdCreationSource eventIdCreationSource, 
            IHashingServiceProvider hashingServiceProvider,
            IOptions<FragmentInMemoryStorageProviderOptions>? options = null)
        {
            _logger = logger;
            _eventIdCreationSource = eventIdCreationSource;
            _hashingServiceProvider = hashingServiceProvider;

            _usedStorageSpace = 0L;
            _storageSpaceUsageLimit = options?.Value?.StorageSpaceUsageLimit ?? FragmentInMemoryStorageProviderOptions.Default.StorageSpaceUsageLimit;

            //unlimited storage space usage
            if (_storageSpaceUsageLimit < 0)
                _storageSpaceUsageLimit = 0;

            _lock = new SemaphoreSlim(initialCount: 1);

            _fragments = new Dictionary<string, InMemoryFragment>();
            _allocations = new List<InMemoryStorageSpaceAllocationToken>();
            _writeOperations = new Dictionary<string, FragmentInMemoryWriteStream>();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="FragmentInMemoryStorageProvider" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~FragmentInMemoryStorageProvider()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// <see langword="true"/> if the storage provider and all associated resources allocations were
        /// released ;otherwise <see langword="false"/>.
        /// </summary>
        public bool Disposed => _disposed;


        /// <summary>
        /// Gets how many bytes still fit into this storage provider.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public long AvailableStorageSpace
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();

                if (_storageSpaceUsageLimit > 0)
                {
                    return _storageSpaceUsageLimit - Interlocked.Read(ref _usedStorageSpace);
                }
                else
                {
                    //TODO: Replace with remaining of max ram capacity
                    return long.MaxValue;
                }
            }
        }

        /// <summary>
        /// Gets how many bytes this storage provider already stores.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public long UsedStorageSpace
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return Interlocked.Read(ref _usedStorageSpace);
            }
        }

        /// <summary>
        /// Gets the maximum allowed storage space usage, in bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public long StorageSpaceUsageLimit
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return _storageSpaceUsageLimit;
            }
        }

        /// <summary>
        /// Gets a collection with hash values of all fragments which are stored by this storage provider.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public IReadOnlyCollection<string> Fragments
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return _fragments.Keys;
            }
        }


        /// <summary>
        /// Gets a collection of all active storage allocations.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public IReadOnlyCollection<IStorageSpaceAllocationToken> Allocations
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();
                return _allocations;
            }
        }

        #endregion

        #region Methods

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
        /// Throws an <see cref="ObjectDisposedException"/> if the storage provider was disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStorageProviderWasNotDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(
                    objectName: GetType().FullName, 
                    message: "In-memory storage provider was already disposed.");
        }

        #endregion

        #region Storage Allocation

        /// <summary>
        /// Virtual allocate a specific amount of storage space.
        /// </summary>
        /// <param name="size">
        /// How many bytes should be allocated.
        /// </param>
        /// <param name="allocationToken">
        /// If <paramref name="allocationToken"/> is not <see langword="null"/>; the specified amount of storage space
        /// is allocated of the resources associated to this allocation token.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="allocationToken"/> is not null and unknown to this <see cref="FragmentInMemoryStorageProvider"/>.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// Less storage space is available than <paramref name="size"/> specifies as needed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Allocation token is not null and was disposed.
        /// </exception>
        private void AllocateStorageSpace(long size, IStorageSpaceAllocationToken? allocationToken = null)
        {
            if (size == 0L)
                return;
            
            if (size < 0L)
                throw new ArgumentOutOfRangeException(nameof(size), "Allocation size can't be negative.");

            if (allocationToken == null)
            {
                if (_storageSpaceUsageLimit == 0L)
                {
                    Interlocked.Add(ref _usedStorageSpace, size);
                    return;
                }

                if (Interlocked.Add(ref _usedStorageSpace, size) > _storageSpaceUsageLimit)
                {
                    throw new StorageSpaceAllocationException(
                        requestedAllocationSize: size,
                        usedStorageSpace: Interlocked.Add(ref _usedStorageSpace, -size),
                        availableStorageSpace: AvailableStorageSpace,
                        storageSpaceUsageLimit: StorageSpaceUsageLimit);
                }
            }
            else
            {
                if (allocationToken.Disposed)
                    throw new ObjectDisposedException(allocationToken.ToString(), "Allocation token already disposed.");

                if (allocationToken is InMemoryStorageSpaceAllocationToken token && _allocations.Contains(token))
                {
                    if (Interlocked.Add(ref token._usedStorageSpace, size) > token.TotalAllocatedStorageSpace)
                    {
                        throw new StorageSpaceAllocationException(
                            requestedAllocationSize: size,
                            usedStorageSpace: Interlocked.Add(ref token._usedStorageSpace, -size),
                            availableStorageSpace: token.AvailableFreeSpace,
                            storageSpaceUsageLimit: token.TotalAllocatedStorageSpace,
                            allocationTokenId: token.ID);
                    }
                }
                else
                {
                    throw new ArgumentException("Unknown allocation token.", nameof(allocationToken));   
                }
            }
        }

        /// <summary>
        /// Virtual deallocate a specific amount of storage space.
        /// </summary>
        /// <param name="size">
        /// How many bytes should be deallocated.
        /// </param>
        /// <param name="allocationToken">
        /// If <paramref name="allocationToken"/> is not <see langword="null"/>; the specified amount of storage space
        /// is deallocated of the resources associated to this allocation token.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="allocationToken"/> is not null and unknown to this <see cref="FragmentInMemoryStorageProvider"/>.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// An attempt is made to deallocate mor than what was previously deallocated.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Allocation token is not null and was disposed.
        /// </exception>
        private void DeallocateStorageSpace(long size, InMemoryStorageSpaceAllocationToken? allocationToken = null)
        {
            if (size == 0L)
                return;
            
            if (size < 0L)
                throw new ArgumentOutOfRangeException(nameof(size), "Deallocation size can't be negative.");

            if (allocationToken == null)
            {
                if (Interlocked.Add(ref _usedStorageSpace, -size) < 0L)
                {
                    throw new StorageSpaceAllocationException(
                        message: "Unable to deallocate more than total amount of allocated bytes.",
                        requestedAllocationSize: -size,
                        usedStorageSpace: Interlocked.Add(ref _usedStorageSpace, +size),
                        availableStorageSpace: AvailableStorageSpace,
                        storageSpaceUsageLimit: StorageSpaceUsageLimit);
                }
            }
            else
            {
                if (allocationToken.Disposed)
                    throw new ObjectDisposedException(allocationToken.ToString(), "Allocation token already disposed.");

                if (_allocations.Contains(allocationToken))
                {
                    if (Interlocked.Add(ref allocationToken._usedStorageSpace, -size) < 0L)
                    {
                        throw new StorageSpaceAllocationException(
                            message: "Unable to deallocate more than total allocation size.",
                            requestedAllocationSize: -size,
                            usedStorageSpace: Interlocked.Add(ref allocationToken._usedStorageSpace, +size),
                            availableStorageSpace: allocationToken.AvailableFreeSpace,
                            storageSpaceUsageLimit: allocationToken.TotalAllocatedStorageSpace,
                            allocationTokenId: allocationToken.ID);
                    }
                }
                else
                {
                    throw new ArgumentException("Unknown allocation token.", nameof(allocationToken));
                }
            }
        }

        ///<summary>
        /// Asynchronously allocates a specific amount of storage.
        /// </summary>
        /// <param name="size">
        /// Amount of storage in bytes that should be allocated.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous allocation operation and wraps the allocation token associated
        /// to the resources that were reserved by this operation.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is negative.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// Throws if the needed resources to allocate could ne be allocated.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public async Task<IStorageSpaceAllocationToken> AllocateStorageSpaceAsync(long size)
        {
            EnsureStorageProviderWasNotDisposed();

            return await InMemoryStorageSpaceAllocationToken.CreateAsync(size, this).ConfigureAwait(false);
        }

        #endregion

        #region Read Operations

        // CS1998 is disabled because the code was nearly unreadable as it was implemented synchronous without async and this
        // will automatically preserve the StackTrace of the ObjectDisposedException. 
#pragma warning disable CS1998 // Asynchronous Method is missing "await"-Operator. The Method will run synchronously.
        /// <summary>
        /// Asynchronously checks if a fragment with a specific hash value is stored.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the file fragment to look for.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation an wraps <see langword="true"/> if a fragment with the
        /// specified hash value is stored; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public async ValueTask<bool> IsFragmentStoredAsync(string fragmentHash)
#pragma warning restore CS1998 // Asynchronous Method is missing "await"-Operator. The Method will run synchronously.
        {
            EnsureStorageProviderWasNotDisposed();

            if (fragmentHash == null || !_hashingServiceProvider.Validate(fragmentHash))
                return false;

            fragmentHash = _hashingServiceProvider.Normalize(fragmentHash);
            return _fragments.ContainsKey(fragmentHash);
        }

        // CS1998 is disabled because the code was nearly unreadable as it was implemented synchronous without async and this
        // will automatically preserve the StackTrace of the ObjectDisposedException. 
#pragma warning disable CS1998 // Asynchronous Method is missing "await"-Operator. The Method will run synchronously.
        /// <summary>
        /// Asynchronously gets the size in bytes of a fragment with specific hash value that is stored.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the file fragment to look for.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation an wraps a positive 64-bit integer that indicates the size of the fragment
        /// with the specified hash value if it is stored; otherwise -1.
        /// (-1 will be returned especially if <paramref name="fragmentHash"/> is
        /// <see langword="null"/> or <paramref name="fragmentHash"/> stores a hash value in an invalid format.)
        /// </returns>
        /// <exception cref="IOException">
        /// The operation to underlying storage system to get the storage size of the specified fragment failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public async ValueTask<long> GetFragmentStorageSizeAsync(string fragmentHash)
#pragma warning restore CS1998 // Asynchronous Method is missing "await"-Operator. The Method will run synchronously.
        {
            EnsureStorageProviderWasNotDisposed();

            if (fragmentHash == null || !_hashingServiceProvider.Validate(fragmentHash))
                return -1;

            fragmentHash = _hashingServiceProvider.Normalize(fragmentHash);

            if (_fragments.TryGetValue(fragmentHash, out InMemoryFragment fragment))
                return fragment.Data.LongLength;

            return -1;
        }

        /// <summary>
        /// Asynchronously gets the byte data of a stored fragment.
        /// </summary>
        /// <remarks>
        /// Use <seealso cref="ReadFragment(string, bool)"/> for large fragment sizes.
        /// If an object is greater than or equal to 85,000 bytes in size, it’s considered a large object and ends up on
        /// the <a href="https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap">large object heap</a>.
        /// </remarks>
        /// <param name="fragmentHash">
        /// The hash value of the fragment whose data should be read.
        /// </param>
        /// <param name="delete">
        /// <see langword="true"/> if the fragment should be deleted from this <see cref="IFragmentStorageProvider"/> after the
        /// fragment data has been read; otherwise <see langword="false"/>.
        /// Remarks: The delete operation is performed after the last reader finished reading. Be aware of that when multiple
        /// threads start reading the same fragment.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is None.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation and wraps the byte data of the specified fragment.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="fragmentHash"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Throws if <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Throws if the <see cref="IFragmentStorageProvider"/> doesn't stores a fragment with the hash <paramref name="fragmentHash"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public async Task<byte[]> GetFragmentAsync(
            string fragmentHash, 
            bool delete = false, 
            CancellationToken? cancellationToken = null)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            EnsureStorageProviderWasNotDisposed();

            if (fragmentHash == null)
                throw new ArgumentNullException(nameof(fragmentHash));

            if (!_hashingServiceProvider.Validate(fragmentHash))
                throw new FormatException("Fragment hash format is invalid.");

            fragmentHash = _hashingServiceProvider.Normalize(fragmentHash);

            try
            {
                await _lock.WaitAsync();

                if (_fragments.TryGetValue(fragmentHash, out InMemoryFragment fragment))
                {
                    if (delete)
                    {
                        await DeleteFragmentAsyncCore(fragmentHash, false);
                    }

                    return fragment.Data;
                }
            }
            finally
            {
                _lock.Release();
            }

            throw new KeyNotFoundException($"No fragment with the specified hash '{fragmentHash}' was found.");
        }

        /// <summary>
        /// Returns a <see cref="Stream"/> to read the byte data of the specified fragment from.
        /// </summary>
        /// <remarks>
        /// You can use <seealso cref="GetFragmentAsync(string, bool, CancellationToken?)"/> to read small fragments.
        /// </remarks>
        /// <param name="fragmentHash">
        /// The hash value of the fragment whose data should be read.
        /// </param>
        /// <param name="delete">
        /// <see langword="true"/> if the fragment should be deleted from this <see cref="IFragmentStorageProvider"/> after the
        /// returned <see cref="Stream"/> was closed; otherwise <see langword="false"/>.
        /// Remarks: The delete operation is performed after the last reader finished reading. Be aware of that when multiple
        /// threads start reading the same fragment.
        /// </param>
        /// <returns>
        /// A <see cref="Stream"/> to read the byte data of the specified fragment from.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="fragmentHash"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Throws if <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Throws if the <see cref="IFragmentStorageProvider"/> doesn't stores a fragment with the hash <paramref name="fragmentHash"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public Stream ReadFragment(string fragmentHash, bool delete = false)
        {
            EnsureStorageProviderWasNotDisposed();

            return FragmentInMemoryReadStream.Create(fragmentHash, delete, this);
        }

        #endregion

        #region Write Operations

        /// <summary>
        /// Asynchronously stores the byte data of a fragment.
        /// </summary>
        /// <remarks>
        /// Use <seealso cref="WriteFragment(string, long, IStorageSpaceAllocationToken?)"/> for large fragment sizes.
        /// 
        /// If an object is greater than or equal to 85,000 bytes in size, it’s considered a large object and ends up on
        /// the <a href="https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap">large object heap</a>.
        /// </remarks>
        /// <param name="fragmentHash">
        /// The hash of the fragment whose data should be stored.
        /// </param>
        /// <param name="data">
        /// The byte data of the fragment to store.
        /// </param>
        /// <param name="allocationToken">
        /// The allocation token for the in advance allocated storage space where the byte data of the specified fragment should be stored.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is None.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="fragmentHash"/> or <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Throws if an fragment with the same hash value already exists in this <see cref="IFragmentStorageProvider"/>.
        /// -or- <paramref name="allocationToken"/> was not created by this storage provider.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// Fragment size is larger than the remaining storage space.
        /// </exception>
        /// <exception cref="IOException">
        /// Throws if an write operation for the fragment with the specified hash value is ongoing.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="allocationToken"/> is not <see langword="null"/> and was already disposed.
        /// -or- Method were called after this storage provider was disposed.
        /// </exception>
        public async Task StoreFragmentAsync(
            string fragmentHash, 
            byte[] data, 
            IStorageSpaceAllocationToken? allocationToken = null, 
            CancellationToken? cancellationToken = null)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            EnsureStorageProviderWasNotDisposed();

            if (fragmentHash == null)
                throw new ArgumentNullException(nameof(fragmentHash));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (!_hashingServiceProvider.Validate(fragmentHash))
                throw new FormatException("Fragment hash format is invalid.");

            fragmentHash = _hashingServiceProvider.Normalize(fragmentHash);

            try
            {
                await _lock.WaitAsync();

                if (_fragments.ContainsKey(fragmentHash))
                    throw new ArgumentException("An fragment with the same hash value already exists", nameof(fragmentHash));

                if (_writeOperations.ContainsKey(fragmentHash))
                    throw new IOException("Unable to write fragment. An write operation is ongoing.");

                AllocateStorageSpace(data.LongLength, allocationToken);

                InMemoryStorageSpaceAllocationToken? token = allocationToken as InMemoryStorageSpaceAllocationToken;
                token?._fragments.Add(fragmentHash);
                _fragments.Add(fragmentHash, new InMemoryFragment(data, token == null, token));
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Returns a stream where the byte data of a fragment can be written to, to store the fragment.
        /// </summary>
        /// <remarks>
        /// You can use <seealso cref="StoreFragmentAsync(string, byte[], IStorageSpaceAllocationToken, CancellationToken?)"/> to
        /// store small fragments.
        /// </remarks>
        /// <param name="fragmentHash">
        /// The hash value of the fragment whose data should be stored.
        /// </param>
        /// <param name="fragmentSize">
        /// Specifies how many bytes the fragment will occupy.
        /// The <see cref="IFragmentStorageProvider"/> can therefore allocate the needed resources or throw an <see cref="IOException"/> if
        /// not enough resources could be allocated.
        /// </param>
        /// <param name="allocationToken">
        /// The allocation token for the in advance allocated storage space where the byte data of the specified fragment should be stored.
        /// </param>
        /// <returns>
        /// A <see cref="Stream"/> where the byte data of a fragment can be written to, to store the fragment.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="fragmentHash"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws if <paramref name="fragmentSize"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Throws if an fragment with the same hash value already exists in this <see cref="IFragmentStorageProvider"/>.
        /// -or- <paramref name="allocationToken"/> was not created by this storage provider.
        /// </exception>
        /// <exception cref="FormatException">
        /// Throws if <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Throws if an write operation for the fragment with the specified hash value is ongoing.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// Fragment size is larger than the remaining storage space.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="allocationToken"/> is not <see langword="null"/> and was already disposed.
        /// -or- Method were called after this storage provider was disposed.
        /// </exception>
        public Stream WriteFragment(string fragmentHash, long fragmentSize, IStorageSpaceAllocationToken? allocationToken = null)
        {
            EnsureStorageProviderWasNotDisposed();

            return FragmentInMemoryWriteStream.Create(fragmentHash, fragmentSize, storageProvider: this, allocationToken);
        }

        /// <summary>
        /// Deletes the stored data of an fragment from this storage provider.
        /// </summary>
        /// <remarks>
        /// Does nothing if no fragment with the specified <paramref name="fragmentHash"/> was found. 
        /// </remarks>
        /// <param name="fragmentHash">
        /// The hash value of the fragment whose data should be deleted.
        /// </param>
        /// <param name="wait">
        /// <see langword="true"/> if this operation should be waiting for all pending readers to finish until the fragment was deleted;
        /// <see langword="false"/> if this operation should finish just after the fragment was marked as to delete.
        /// The last pending reader who reads the specified fragment will delete the fragment then.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="fragmentHash"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Throws if <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public async Task DeleteFragmentAsync(string fragmentHash, bool wait = false)
        {
            EnsureStorageProviderWasNotDisposed();

            if (fragmentHash == null)
                throw new ArgumentNullException(nameof(fragmentHash));

            if (!_hashingServiceProvider.Validate(fragmentHash))
                throw new FormatException("Invalid fragment hash format.");

            fragmentHash = _hashingServiceProvider.Normalize(fragmentHash);

            try
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                await DeleteFragmentAsyncCore(fragmentHash, wait).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// TODO: DOCUMENT "private Task DeleteFragmentAsyncCore(string fragmentHash, bool wait)"
        /// </summary>
        /// <param name="fragmentHash">
        ///
        /// </param>
        /// <param name="wait">
        ///
        /// </param>
        /// <returns>
        ///
        /// </returns>
        private Task DeleteFragmentAsyncCore(string fragmentHash, bool wait)
        {
            if (_fragments.TryGetValue(fragmentHash, out InMemoryFragment fragmentMetadata))
            {
                if (fragmentMetadata.ReadOperations.Count == 0)
                {
                    _fragments.Remove(fragmentHash);
                    fragmentMetadata.AllocationToken?._fragments.Remove(fragmentHash);
                    DeallocateStorageSpace(fragmentMetadata.Data.LongLength, fragmentMetadata.AllocationToken);

                    fragmentMetadata.FragmentRemovalTaskSource?.SetResult(null);
                }
                else
                {
                    fragmentMetadata.Remove = true;

                    if (wait)
                    {
                        fragmentMetadata.FragmentRemovalTaskSource ??= new TaskCompletionSource<object?>();
                        return fragmentMetadata.FragmentRemovalTaskSource.Task;
                    }
                }
            }

            return Task.CompletedTask;
        }

        #endregion

        /// <summary>
        /// Releases all allocated resources associated to this storage provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously releases the resources used by this <see cref="FragmentInMemoryStorageProvider" /> instance.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous dispose operation.
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (InMemoryStorageSpaceAllocationToken allocationToken in _allocations)
            {
                await allocationToken.DisposeAsync();
            }

            foreach (FragmentInMemoryWriteStream writeOperation in _writeOperations.Values)
            {
                await writeOperation.DisposeAsync();
            }

            foreach (InMemoryFragment fragment in _fragments.Values)
            {
                foreach (FragmentInMemoryReadStream readOperation in fragment.ReadOperations)
                {
                    await readOperation.DisposeAsync();
                }
            }

            _fragments.Clear();
            _writeOperations.Clear();
            _allocations.Clear();
            _lock.Dispose();

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (InMemoryStorageSpaceAllocationToken allocationToken in _allocations)
            {
                allocationToken.Dispose();
            }

            foreach (FragmentInMemoryWriteStream writeOperation in _writeOperations.Values)
            {
                writeOperation.Dispose();
            }

            foreach (InMemoryFragment fragment in _fragments.Values)
            {
                foreach (FragmentInMemoryReadStream readOperation in fragment.ReadOperations)
                {
                    readOperation.Dispose();
                }
            }

            if (disposing)
            {
                _fragments.Clear();
                _writeOperations.Clear();
                _allocations.Clear();
                _lock.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

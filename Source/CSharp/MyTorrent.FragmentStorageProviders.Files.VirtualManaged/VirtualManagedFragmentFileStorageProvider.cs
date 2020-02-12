using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTorrent.HashingServiceProviders;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// An <see cref="IFragmentStorageProvider"/> that stores the fragment data on the filesystem an manages the resource allocation virtual.
    /// No physical allocations on disk are happening.
    /// </summary>
    /// <remarks>
    /// WARNING: Because the resource management is handled virtually, this storage provider is not suitable for
    ///          competing with other processes in the same storage folder. 
    /// </remarks>
    public sealed partial class VirtualManagedFragmentFileStorageProvider : IFragmentStorageProvider
    {
        #region Private Variables

        private const string FragmentFileExtension = ".bin";
        private const string TemporaryFileExtension = ".tmp";

        //TODO: IMPLEMENT Log Messages
        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<VirtualManagedFragmentFileStorageProvider> _logger;

        private readonly IHashingServiceProvider _hashingServiceProvider;

        /// <summary>
        /// Locking mechanism to ensure thread safety.
        /// </summary>
        private readonly SemaphoreSlim _lock;

        /// <summary>
        /// List that contains all non-disposed allocations.
        /// </summary>
        private readonly List<VirtualManagedFileStorageSpaceAllocationToken> _allocations;

        /// <summary>
        /// List with the hash values of all fragments which are currently written and therefore can not be read from or deleted.
        /// </summary>
        private readonly Dictionary<string, VirtualManagedFragmentFileWriteStream> _writeOperations;

        /// <summary>
        /// Dictionary with the hash values of all fragments and their meta data.
        /// </summary>
        private readonly Dictionary<string, FragmentMetadata> _fragments;

        /// <summary>
        /// How many bytes already have been allocated.
        /// </summary>
        private long _usedStorageSpace;

        /// <summary>
        /// The Limit how many bytes can be allocated.
        /// </summary>
        private readonly long _storageSpaceUsageLimit;

        private readonly DriveInfo _driveInfo;
        private readonly DirectoryInfo _storageFolder;
        private readonly DirectoryInfo _tmpStorageFolder;

        private volatile bool _disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="VirtualManagedFragmentFileStorageProvider"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger that should be used for this <see cref="VirtualManagedFragmentFileStorageProvider"/> instance.
        /// </param>
        /// <param name="eventIdCreationSource">
        /// The source for creating unique event Id's that should be used by this <see cref="VirtualManagedFragmentFileStorageProvider"/> instance.
        /// </param>
        /// <param name="hashingServiceProvider">
        /// The service provider that validates und normalizes hashes and should be used by this <see cref="VirtualManagedFragmentFileStorageProvider"/> instance.
        /// </param>
        /// <param name="options">
        /// The options to configure this <see cref="VirtualManagedFragmentFileStorageProvider"/> instance.
        /// </param>
        public VirtualManagedFragmentFileStorageProvider(
            ILogger<VirtualManagedFragmentFileStorageProvider> logger,
            IEventIdCreationSource eventIdCreationSource,
            IHashingServiceProvider hashingServiceProvider,
            IOptions<VirtualManagedFragmentFileStorageProviderOptions>? options = null)
        {
            _logger = logger;
            _eventIdCreationSource = eventIdCreationSource;
            _hashingServiceProvider = hashingServiceProvider;

            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, "Initializing VirtualManaged-Fragment-FileStorage-Provider.");

            options ??= Options.Create(VirtualManagedFragmentFileStorageProviderOptions.Default);

            _usedStorageSpace = 0L;
            _storageSpaceUsageLimit = options.Value?.StorageSpaceUsageLimit
                                      ?? VirtualManagedFragmentFileStorageProviderOptions.Default.StorageSpaceUsageLimit;

            //unlimited storage space usage
            if (_storageSpaceUsageLimit < 0)
                _storageSpaceUsageLimit = 0;

            _lock = new SemaphoreSlim(initialCount: 1);

            _fragments = new Dictionary<string, FragmentMetadata>();
            _allocations = new List<VirtualManagedFileStorageSpaceAllocationToken>();
            _writeOperations = new Dictionary<string, VirtualManagedFragmentFileWriteStream>();

            string storageFolderPath = options.Value?.StorageFolderPath
                                       ?? VirtualManagedFragmentFileStorageProviderOptions.Default.StorageFolderPath
                                       ?? throw new ArgumentNullException(nameof(VirtualManagedFragmentFileStorageProviderOptions.StorageFolderPath));

            _storageFolder = new DirectoryInfo(storageFolderPath);
            _tmpStorageFolder = new DirectoryInfo(Path.Combine(storageFolderPath, "temp"));
            _driveInfo = new DriveInfo(_storageFolder.Root.FullName);

            bool resetOnStartup = options.Value?.ResetOnStartup ?? VirtualManagedFragmentFileStorageProviderOptions.Default.ResetOnStartup;
            
            if (_storageFolder.Exists)
            {
                _logger.LogInformation(eventId, $"Storage Folder found. Using: '{_storageFolder.FullName}'");

                if (resetOnStartup)
                {
                    #region Cleanup persistent storage
                    
                    _logger.LogInformation(eventId, "Reset on Startup configured. Deleting persistent fragments from storage.");

                    foreach (FileInfo fragmentFile in _storageFolder.GetFiles("*" + FragmentFileExtension, SearchOption.TopDirectoryOnly))
                    {
#if TRACE
                        _logger.LogTrace(eventId, $"Deleting file: '{fragmentFile.Name}' ({fragmentFile.FullName})");
#endif
                        fragmentFile.Delete();
                    }
#if DEBUG
                    _logger.LogDebug(eventId, "Reset on startup completed. Deleted all persistent fragments from storage.");
#endif 
                    #endregion
                }
                else
                {
                    #region Load persistent storage

                    _logger.LogInformation(eventId, "Loading existing persistent fragments from storage.");

                    foreach (FileInfo fragmentFile in _storageFolder.GetFiles("*" + FragmentFileExtension, SearchOption.TopDirectoryOnly))
                    {
#if TRACE
                        _logger.LogTrace(eventId, $"Loading file info: '{fragmentFile.Name}' ({fragmentFile.FullName})");
#endif
                        string fragmentHash = fragmentFile.Name;
                        fragmentHash = fragmentHash.Substring(0, fragmentHash.Length - FragmentFileExtension.Length);

                        if (!_hashingServiceProvider.Validate(fragmentHash))
                        {
                            _logger.LogWarning($"Skipping fragment file named with an invalid hash format: '{fragmentFile.Name}' ({fragmentFile.FullName})");
                            continue;
                        }

                        if (!fragmentHash.Equals(_hashingServiceProvider.Normalize(fragmentHash)))
                        {
                            _logger.LogWarning($"Normalizing name of fragment file with non normalized file name: '{fragmentFile.Name}' ({fragmentFile.FullName})");

                            fragmentHash = _hashingServiceProvider.Normalize(fragmentHash);
                            fragmentFile.MoveTo(GetCommittedFragmentPath(fragmentHash));
                        }

                        _fragments.Add(fragmentHash, new FragmentMetadata(fragmentHash, fragmentFile.Length, persistent: true));
                        _usedStorageSpace += fragmentFile.Length;
                    }
#if DEBUG
                    _logger.LogDebug(eventId, "Completed loading existing persistent fragments from storage.");
#endif
                    #endregion
                }

                #region Cleanup non persistent storage

                if (_tmpStorageFolder.Exists)
                {
                    _logger.LogInformation(eventId, "Deleting non-persistent fragments from storage.");

                    foreach (var fragmentFile in _tmpStorageFolder.GetFiles("*" + FragmentFileExtension, SearchOption.TopDirectoryOnly))
                    {
#if TRACE
                        _logger.LogTrace(eventId, $"Deleting file: '{fragmentFile.Name}' ({fragmentFile.FullName})");
#endif
                        fragmentFile.Delete();
                    }

                    foreach (var fragmentFile in _tmpStorageFolder.GetFiles("*" + TemporaryFileExtension, SearchOption.TopDirectoryOnly))
                    {
#if TRACE
                        _logger.LogTrace(eventId, $"Deleting file: '{fragmentFile.Name}' ({fragmentFile.FullName})");
#endif
                        fragmentFile.Delete();
                    }
#if DEBUG
                    _logger.LogDebug(eventId, "Deleted all non-persistent fragments from storage.");
#endif
                }
                else
                {
                    _storageFolder.Create();
                    _logger.LogInformation(eventId, $"Temporary Storage Folder not found. Created: '{_tmpStorageFolder.FullName}'");
                }

                #endregion
            }
            else
            {
                _storageFolder.Create();
                _logger.LogInformation(eventId, $"Storage Folder not found. Created: '{_storageFolder.FullName}'");
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="VirtualManagedFragmentFileStorageProvider" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~VirtualManagedFragmentFileStorageProvider()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// <see langword="true"/> if the storage provider and all associated resources allocations were
        /// released; otherwise <see langword="false"/>.
        /// </summary>
        public bool Disposed => _disposed;

        /// <summary>
        /// Gets how many bytes still fit into this storage provider.
        /// </summary>
        /// <remarks>
        /// Sum of <see cref="IFragmentStorageProvider.AvailableStorageSpace"/> and <see cref="IFragmentStorageProvider.UsedStorageSpace"/> don't have to be constant.
        ///
        /// For Example:
        /// If fragment data is stored on a drive of the filesystem and an other process stores data on the same
        /// drive the <see cref="IFragmentStorageProvider.AvailableStorageSpace"/> can change without any write operations of this
        /// <see cref="IFragmentStorageProvider"/>. 
        /// </remarks>
        /// <exception cref="IOException">
        /// Failed to read available storage space from drive info.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public long AvailableStorageSpace
        {
            get
            {
                EnsureStorageProviderWasNotDisposed();

                try
                {
                    long available = _driveInfo.AvailableFreeSpace;

                    if (_storageSpaceUsageLimit > 0)
                        return Math.Min(available, _storageSpaceUsageLimit - Interlocked.Read(ref _usedStorageSpace));
                    
                    return available;
                }
                catch (Exception exception)
                {
                    const string errorMessage = "Failed to read available storage space from drive info.";
                    
                    _logger.LogError(exception, errorMessage);
                    throw new IOException(errorMessage, exception);
                }
            }
        }

        /// <summary>
        /// Gets how many bytes this storage provider already stores.
        /// </summary>
        /// <remarks>
        /// Sum of <see cref="IFragmentStorageProvider.AvailableStorageSpace"/> and <see cref="IFragmentStorageProvider.UsedStorageSpace"/> don't have to be constant.
        ///
        /// For Example:
        /// If fragment data is stored on a drive of the filesystem and an other process stores data on the same
        /// drive the <see cref="IFragmentStorageProvider.AvailableStorageSpace"/> can change without any write operations of this
        /// <see cref="IFragmentStorageProvider"/>. 
        /// </remarks>
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
        /// <remarks>
        /// If 0 is returned, no storage space usage limit was defined.
        /// </remarks>
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// This Operation is not supported by the storage provider.
        /// </exception>
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
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
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
        /// <exception cref="IOException">
        /// The operation to underlying storage system failed.
        /// </exception>
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
        /// Ensures that the storage provider was not disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStorageProviderWasNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    objectName: GetType().FullName,
                    message: "Virtual-managed file storage provider was already disposed.");
        }

        /// <summary>
        /// Ensures that the folders where the fragment files are stored exists and creates the if they are missing.
        /// </summary>
        /// <param name="eventId">
        /// The eventId of the operation that calls this code contract validation
        /// </param>
        /// <exception cref="IOException">
        /// Failed to create storage folders.
        /// </exception>
        private void EnsureStorageFoldersExist(EventId? eventId = null)
        {
            if (_storageFolder.Exists && _tmpStorageFolder.Exists) 
                return;
            
            try
            {
                if (!_storageFolder.Exists)
                    _storageFolder.Create();

                if (!_tmpStorageFolder.Exists)
                    _tmpStorageFolder.Create();
            }
            catch (Exception exception)
            {
                const string errorMessage = "Failed to create storage folder.";

                if (eventId.HasValue)
                    _logger.LogCritical(eventId.Value, exception, errorMessage);
                else
                    _logger.LogCritical(exception, errorMessage);

                throw new IOException(errorMessage, exception);
            }
        }

        /// <summary>
        /// Gets the path for a persistent fragment file with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// Specific hash value of the fragment.
        /// </param>
        /// <returns>
        /// The path of the fragment file as <see cref="string"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetCommittedFragmentPath(string fragmentHash) => Path.Combine(_storageFolder.FullName, fragmentHash + FragmentFileExtension);

        /// <summary>
        /// Gets the path for a non-persistent fragment file with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// Specific hash value of the fragment.
        /// </param>
        /// <returns>
        /// The path of the fragment file as <see cref="string"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetNonCommittedFragmentPath(string fragmentHash) => Path.Combine(_tmpStorageFolder.FullName, fragmentHash + FragmentFileExtension);

        /// <summary>
        /// Gets the path for a fragment file with a specific hash value that is written to.
        /// </summary>
        /// <param name="fragmentHash">
        /// Specific hash value of the fragment.
        /// </param>
        /// <returns>
        /// The path of the fragment file as <see cref="string"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetTempFragmentPath(string fragmentHash) => Path.Combine(_tmpStorageFolder.FullName, fragmentHash + FragmentFileExtension + ".tmp");

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
        /// <paramref name="allocationToken"/> is not null and unknown to this <see cref="VirtualManagedFileStorageSpaceAllocationToken"/>.
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

                if (allocationToken is VirtualManagedFileStorageSpaceAllocationToken token && _allocations.Contains(token))
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
        /// <paramref name="allocationToken"/> is not null and unknown to this <see cref="VirtualManagedFileStorageSpaceAllocationToken"/>.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// An attempt is made to deallocate mor than what was previously deallocated.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Allocation token is not null and was disposed.
        /// </exception>
        private void DeallocateStorageSpace(long size, VirtualManagedFileStorageSpaceAllocationToken? allocationToken = null)
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
                    throw new ObjectDisposedException(allocationToken.ToString(), "Allocation token was already disposed.");

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

        /// <summary>
        /// Asynchronously allocates a specific amount of storage.
        /// </summary>
        /// <param name="size">
        /// Amount of storage in bytes that should be allocated.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous allocation operation and wraps the
        /// allocation token associated to the resources that were reserved by this operation.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is negative.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// Throws if the needed resources to allocate could ne be allocated.
        /// </exception>
        /// <exception cref="IOException">
        /// Throws if the operation to underlying storage system to allocate the specified space failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public async Task<IStorageSpaceAllocationToken> AllocateStorageSpaceAsync(long size)
        {
            EnsureStorageProviderWasNotDisposed();
            return await VirtualManagedFileStorageSpaceAllocationToken.CreateAsync(size, this).ConfigureAwait(false);
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
        /// A task that represents the asynchronous operation an wraps <see langword="true"/> if a fragment
        /// with the specified hash value is stored; otherwise <see langword="false"/>.
        /// (<see langword="false"/> will be returned especially if <paramref name="fragmentHash"/> is
        /// <see langword="null"/> or <paramref name="fragmentHash"/> stores a hash value in an invalid format.)
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

            if (_fragments.TryGetValue(fragmentHash, out FragmentMetadata fragmentMetadata))
                return fragmentMetadata.Size;

            return -1;
        }

        /// <summary>
        /// Asynchronously gets the byte data of a stored fragment.
        /// </summary>
        /// <remarks>
        /// Use <seealso cref="IFragmentStorageProvider.ReadFragment"/> for large fragment sizes.
        /// If an object is greater than or equal to 85,000 bytes in size, it’s considered a 
        /// large object and ends up on the <a href="https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap">large object heap</a>.
        /// </remarks>
        /// <param name="fragmentHash">
        /// The hash value of the fragment whose data should be read.
        /// </param>
        /// <param name="delete">
        /// <see langword="true"/> if the fragment should be deleted from this <see cref="IFragmentStorageProvider"/>
        /// after the fragment data has been read; otherwise <see langword="false"/>.
        /// Remarks: The delete operation is performed after the last reader finished reading. Be aware of that when
        /// multiple threads start reading the same fragment.
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
        /// <exception cref="IOException">
        /// The operation to underlying storage system to read the fragment data failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public async Task<byte[]> GetFragmentAsync(string fragmentHash, bool delete = false, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            await using (Stream readStream = ReadFragment(fragmentHash, delete))
            {
                byte[] buffer = new byte[readStream.Length];

                await using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    try
                    {
                        await readStream.CopyToAsync(memoryStream, cancellationToken.Value);
                    }
                    catch (Exception exception) when (!(exception is OperationCanceledException))
                    {
                        throw new IOException("Failed to read fragment data.", exception);
                    }
                }

                return buffer;
            }
        }

        /// <summary>
        /// Returns a <see cref="Stream"/> to read the byte data of the specified fragment from.
        /// </summary>
        /// <remarks>
        /// You can use <seealso cref="IFragmentStorageProvider.GetFragmentAsync"/> to read small fragments.
        /// </remarks>
        /// <param name="fragmentHash">
        /// The hash value of the fragment whose data should be read.
        /// </param>
        /// <param name="delete">
        /// <see langword="true"/> if the fragment should be deleted from this <see cref="IFragmentStorageProvider"/> after
        /// the returned <see cref="Stream"/> was closed; otherwise <see langword="false"/>.
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
        /// <exception cref="IOException">
        /// The operation to the underlying storage system failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this storage provider was disposed.
        /// </exception>
        public Stream ReadFragment(string fragmentHash, bool delete = false)
        {
            EnsureStorageProviderWasNotDisposed();

            return VirtualManagedFragmentFileReadStream.Create(fragmentHash, delete, this);
        }

        #endregion

        #region Write Operations

        /// <summary>
        /// Asynchronously stores the byte data of a fragment.
        /// </summary>
        /// <remarks>
        /// Use <seealso cref="IFragmentStorageProvider.WriteFragment"/> for large fragment sizes.
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
        /// <exception cref="FormatException">
        /// Throws if <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="StorageSpaceAllocationException">
        /// Fragment size is larger than the remaining storage space.
        /// </exception>
        /// <exception cref="IOException">
        /// The operation to underlying storage system to store the fragment data failed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="allocationToken"/> is not <see langword="null"/> and was already disposed.
        /// -or- Method were called after this storage provider was disposed.
        /// </exception>
        public async Task StoreFragmentAsync(string fragmentHash, byte[] data, IStorageSpaceAllocationToken? allocationToken = null,
            CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            await using Stream writeStream = WriteFragment(fragmentHash, data.LongLength, allocationToken);
            await using MemoryStream memoryStream = new MemoryStream(data);
            
            try
            {
                await memoryStream.CopyToAsync(writeStream, cancellationToken.Value);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                throw new IOException("Failed to write fragment data!", exception);
            }
        }

        /// <summary>
        /// Returns a stream where the byte data of a fragment can be written to, to store the fragment.
        /// </summary>
        /// <remarks>
        /// You can use <seealso cref="IFragmentStorageProvider.StoreFragmentAsync"/> to store small fragments.
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
        /// <exception cref="FormatException">
        /// Throws if <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws if <paramref name="fragmentSize"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Throws if an fragment with the same hash value already exists in this <see cref="IFragmentStorageProvider"/>.
        /// -or- <paramref name="allocationToken"/> was not created by this storage provider.
        /// </exception>
        /// <exception cref="IOException">
        /// The operation to underlying storage system to store the fragment data failed.
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
            EnsureStorageFoldersExist();

            return VirtualManagedFragmentFileWriteStream.Create(fragmentHash, fragmentSize, this, allocationToken);
        }

        /// <summary>
        /// Deletes the stored data of an fragment from this <see cref="IFragmentStorageProvider"/>.
        /// </summary>
        /// <remarks>
        /// Does nothing if no fragment with the specified <paramref name="fragmentHash"/> was found or the value represents no valid hash. 
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
        /// <exception cref="IOException">
        /// The operation to underlying storage system to delete the fragment failed.
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
                await _lock.WaitAsync();
                await DeleteFragmentAsyncCore(fragmentHash, wait);
            }
            finally
            {
                _lock.Release();
            }
        }

        private Task DeleteFragmentAsyncCore(string fragmentHash, bool wait)
        {
            if (_fragments.TryGetValue(fragmentHash, out FragmentMetadata fragmentMetadata))
            {
                if (fragmentMetadata.ReadOperations.Count == 0)
                {
                    string path = fragmentMetadata.Persistent ? GetCommittedFragmentPath(fragmentHash) : GetNonCommittedFragmentPath(fragmentHash);

                    try
                    {
                        File.Delete(path);
                        _fragments.Remove(fragmentHash);
                        fragmentMetadata.AllocationToken?._fragments.Remove(fragmentHash);
                        DeallocateStorageSpace(fragmentMetadata.Size, fragmentMetadata.AllocationToken);

                        fragmentMetadata.FragmentRemovalTaskSource?.SetResult(null);
                    }
                    catch (Exception exception)
                    {
                        //Workaround to set the stacktrace of the exception.
                        try
                        {
                            throw new IOException($"Failed to delete fragment file from filesystem.{Environment.NewLine}    (File: {path})", exception);
                        }
                        catch (Exception resultException)
                        {
                            fragmentMetadata.FragmentRemovalTaskSource?.SetException(resultException);
                            return Task.FromException(resultException);
                        }
                    }
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
        /// Releases all allocated resources associated to this <see cref="VirtualManagedFragmentFileStorageProvider" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously releases the resources used by this <see cref="VirtualManagedFragmentFileStorageProvider" /> instance.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous dispose operation.
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (VirtualManagedFileStorageSpaceAllocationToken allocationToken in _allocations)
            {
                await allocationToken.DisposeAsync();
            }

            foreach (VirtualManagedFragmentFileWriteStream writeOperation in _writeOperations.Values)
            {
                await writeOperation.DisposeAsync();
            }

            foreach (FragmentMetadata fragmentMetadata in _fragments.Values)
            {
                foreach (VirtualManagedFragmentFileReadStream readOperation in fragmentMetadata.ReadOperations)
                {
                    await readOperation.DisposeAsync();
                }
            }

            _fragments.Clear();
            _writeOperations.Clear();

            try
            {
                _tmpStorageFolder.Delete(true);
            }
            catch
            {
                //Do Nothing... Dispose throws no exceptions
                //TODO: IMPLEMENT log warning
            }

            _lock.Dispose();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="VirtualManagedFragmentFileStorageProvider" /> and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
        /// release only unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (VirtualManagedFileStorageSpaceAllocationToken allocationToken in _allocations)
            {
                allocationToken.Dispose();
            }

            foreach (VirtualManagedFragmentFileWriteStream writeOperation in _writeOperations.Values)
            {
                writeOperation.Dispose();
            }

            foreach (FragmentMetadata fragmentMetadata in _fragments.Values)
            {
                foreach (VirtualManagedFragmentFileReadStream readOperation in fragmentMetadata.ReadOperations)
                {
                    readOperation.Dispose();
                }
            }

            _fragments.Clear();
            _writeOperations.Clear();
            
            try
            {
                _tmpStorageFolder.Delete(true);
            }
            catch
            {
                //Do Nothing... Dispose throws no exceptions
                //TODO: IMPLEMENT log warning
            }

            if (disposing)
            {
                _lock.Dispose();
            }  
        }
        #endregion
    }
}

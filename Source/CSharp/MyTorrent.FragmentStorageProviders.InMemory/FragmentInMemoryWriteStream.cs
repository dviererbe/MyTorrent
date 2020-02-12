using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    public partial class FragmentInMemoryStorageProvider
    {
        internal class FragmentInMemoryWriteStream : MemoryStream
        {
            private volatile bool _disposed = false;

            private readonly FragmentInMemoryStorageProvider _storageProvider;
            private readonly InMemoryStorageSpaceAllocationToken? _allocationToken;

            private FragmentInMemoryWriteStream(
                string fragmentHash, long fragmentSize, 
                FragmentInMemoryStorageProvider fragmentInMemoryStorageProvider,
                InMemoryStorageSpaceAllocationToken? allocationToken = null)
                : base(new byte[fragmentSize], true)
            {
                _storageProvider = fragmentInMemoryStorageProvider;
                _allocationToken = allocationToken;

                FragmentHash = fragmentHash;
                
                base.SetLength(0);
            }

            public static FragmentInMemoryWriteStream Create(
                string fragmentHash,
                long fragmentSize,
                FragmentInMemoryStorageProvider storageProvider,
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

                    var token = allocationToken as InMemoryStorageSpaceAllocationToken;

                    var writeStream = new FragmentInMemoryWriteStream(
                        fragmentHash,
                        fragmentSize,
                        storageProvider,
                        token);

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
            /// Frees resources before it is reclaimed by garbage collection.
            /// </summary>
            ~FragmentInMemoryWriteStream()
            {
                Dispose(false);
            }

            /// <summary>
            /// Gets if the <see cref="FragmentInMemoryWriteStream"/> was disposed.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the <see cref="FragmentInMemoryWriteStream"/> instance was disposed; otherwise <see langword="false"/>.
            /// </returns>
            public bool Disposed => _disposed;

            /// <summary>
            /// Gets the normalizes fragment hash value of the fragment that is written to.
            /// </summary>
            public string FragmentHash { get; }

            /// <summary>
            /// Should return the array of unsigned bytes from which this stream was created, but this method was overwritten to
            /// prevent access to the buffer, because with a reference to the buffer you could manipulate the data content even
            /// after the Stream was closed.
            /// </summary>
            /// <exception cref="UnauthorizedAccessException">
            /// Throws if this method is called to prevent access to the buffer, because this would violate the readonly nature of this <see cref="Stream"/>. 
            /// </exception>
            [DoesNotReturn]
            public override byte[] GetBuffer()
            {
                throw new UnauthorizedAccessException("Access to the buffer is not allowed.");
            }

            /// <summary>
            /// Should return the array of unsigned bytes from which this stream was created, but this method was overwritten to
            /// prevent access to the buffer, because with a reference to the buffer you could manipulate the data content even
            /// after the Stream was closed.
            /// </summary>
            /// <param name="buffer">
            /// Returns an empty byte array.
            /// </param>
            /// <returns>
            /// Returns <see langword="false"/>, because the buffer is not exposable.
            /// </returns>
            public override bool TryGetBuffer(out ArraySegment<byte> buffer)
            {
                buffer = Array.Empty<byte>();
                return false;
            }

            /// <summary>
            /// Asynchronously releases the resources used by the <see cref="FragmentInMemoryWriteStream" />.
            /// </summary>
            /// <returns>
            /// A task that represents the asynchronous dispose operation.
            /// </returns>
            public override async ValueTask DisposeAsync()
            {
                if (_disposed)
                    return;

                try
                {
                    await _storageProvider._lock.WaitAsync();
                    ReleaseResources();
                }
                finally
                {
                    _storageProvider._lock.Release();
                }

                await base.DisposeAsync();
            }

            /// <summary>
            /// Releases the unmanaged resources used by the <see cref="FragmentInMemoryWriteStream" /> and
            /// optionally releases the managed resources.
            /// </summary>
            /// <param name="disposing">
            /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
            /// release only unmanaged resources.
            /// </param>
            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                try
                {
                    _storageProvider._lock.Wait();
                    ReleaseResources();
                }
                finally
                {
                    _storageProvider._lock.Release();
                }

                base.Dispose(disposing);
            }

            /// <summary>
            /// TODO: DOCUMENT "private void ReleaseResources()"
            /// </summary>
            /// <returns>
            ///
            /// </returns>
            private void ReleaseResources()
            {
                if (!_disposed)
                {
                    _disposed = true;

                    base.Close();

                    _storageProvider._writeOperations.Remove(FragmentHash);

                    if (_allocationToken != null)
                    {
                        _allocationToken._unwrittenFragments.Remove(FragmentHash);

                        if (_allocationToken.Disposed)
                            goto Destroy;
                    }

                    if (Length != Capacity)
                        goto Destroy;

                    if (_allocationToken != null)
                    {
                        _storageProvider._fragments.Add(FragmentHash, new InMemoryFragment(ToArray(), false, _allocationToken));
                        _allocationToken?._fragments.Add(FragmentHash);
                    }
                    else
                    {
                        _storageProvider._fragments.Add(FragmentHash, new InMemoryFragment(ToArray(), true, _allocationToken));
                    }

                    return;
                
                    Destroy:
                    if (_allocationToken != null && !_allocationToken.Disposed)
                        _storageProvider.DeallocateStorageSpace(Capacity, _allocationToken);
                    else
                        _storageProvider.DeallocateStorageSpace(Capacity);
                }
            }
        }
    }
}

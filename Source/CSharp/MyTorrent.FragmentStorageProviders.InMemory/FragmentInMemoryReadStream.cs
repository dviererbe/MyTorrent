using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    public partial class FragmentInMemoryStorageProvider
    {
        internal class FragmentInMemoryReadStream : MemoryStream
        {
            private volatile bool _disposed = false;
            private readonly FragmentInMemoryStorageProvider _storageProvider;

            private FragmentInMemoryReadStream(string fragmentHash, byte[] fragmentData, FragmentInMemoryStorageProvider fragmentInMemoryStorageProvider)
                : base(fragmentData, false)
            {
                _storageProvider = fragmentInMemoryStorageProvider;
                FragmentHash = fragmentHash;
            }

            /// <summary>
            /// Frees resources before it is reclaimed by garbage collection.
            /// </summary>
            ~FragmentInMemoryReadStream()
            {
                Dispose(false);
            }

            /// <summary>
            /// TODO: DOCUMENT "public static FragmentInMemoryReadStream Create(string fragmentHash, bool delete, FragmentInMemoryStorageProvider storageProvider)"
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
            public static FragmentInMemoryReadStream Create(
                string fragmentHash,
                bool delete,
                FragmentInMemoryStorageProvider storageProvider)
            {
                if (fragmentHash == null)
                    throw new ArgumentNullException(nameof(fragmentHash));

                if (!storageProvider._hashingServiceProvider.Validate(fragmentHash))
                    throw new FormatException("Invalid fragment hash format.");

                fragmentHash = storageProvider._hashingServiceProvider.Normalize(fragmentHash);

                try
                {
                    storageProvider._lock.Wait();

                    if (storageProvider._fragments.TryGetValue(fragmentHash, out InMemoryFragment fragment))
                    {
                        FragmentInMemoryReadStream readStream = new FragmentInMemoryReadStream(fragmentHash, fragment.Data, storageProvider);

                        fragment.ReadOperations.Add(readStream);

                        if (delete)
                            fragment.Remove = true;

                        return readStream;
                    }
                }
                finally
                {
                    storageProvider._lock.Release();
                }

                throw new KeyNotFoundException($"No fragment with the specified hash '{fragmentHash}' was found.");
            }

            /// <summary>
            /// Gets if the <see cref="FragmentInMemoryReadStream"/> was disposed.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the <see cref="FragmentInMemoryReadStream"/> instance was disposed; otherwise <see langword="false"/>.
            /// </returns>
            public bool Disposed => _disposed;

            /// <summary>
            /// Gets the normalizes fragment hash value of the fragment that is read from.
            /// </summary>
            public string FragmentHash { get; }

            /// <summary>
            /// Should return the array of unsigned bytes from which this stream was created, but this method was overwritten to
            /// prevent access to the buffer, because this would violate the readonly nature of this <see cref="Stream"/>.
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
            /// prevent access to the buffer, because this would violate the readonly nature of this <see cref="Stream"/>.
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
            /// Releases the unmanaged resources used by the <see cref="FragmentInMemoryReadStream" /> and
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

                _disposed = true;

                try
                {
                    _storageProvider._lock.Wait();

                    var fragment = _storageProvider._fragments[FragmentHash];
                    fragment.ReadOperations.Remove(this);

                    if (fragment.Remove && fragment.ReadOperations.Count == 0)
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

                base.Dispose(disposing);
            }
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ConsumerProducerLocking
{
    /// <summary>
    /// TODO: DOCUMENT ConsumerProducerLock
    /// </summary>
    public sealed class ConsumerProducerLock : IDisposable
    {
        private TaskCompletionSource<object?>? _lastReaderExited;
        private TaskCompletionSource<object?>? _readLockDisabled;
        private readonly SemaphoreSlim _readLock = new SemaphoreSlim(initialCount: 1);
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(initialCount: 1);

        private volatile bool _disposed = false;

        /// <summary>
        /// 
        /// </summary>
        ~ConsumerProducerLock()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanRead { get; private set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public int ReaderCount { get; private set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public ReadSession CreateReadSession()
        {
            EnsureNotDisposed();

            EnterRead();
            return new ReadSession(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public async Task<ReadSession> CreateReadSessionAsync()
        {
            EnsureNotDisposed();

            await EnterReadAsync();
            return new ReadSession(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public WriteSession CreateWriteSession()
        {
            EnsureNotDisposed();

            EnterWrite();
            return new WriteSession(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public async Task<WriteSession> CreateWriteSessionAsync()
        {
            EnsureNotDisposed();

            await EnterWriteAsync();
            return new WriteSession(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public void EnterRead()
        {
            EnsureNotDisposed();
        Start:
            try
            {
                _readLock.Wait();
                
                if (CanRead)
                {
                    ++ReaderCount;
                    return;
                }
            }
            finally
            {
                _readLock.Release();
            }

            _readLockDisabled?.Task.Wait();
            goto Start;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public async Task EnterReadAsync()
        {
            EnsureNotDisposed();

        Start:
            try
            {
                await _readLock.WaitAsync().ConfigureAwait(false);
                
                if (CanRead)
                {
                    ++ReaderCount;
                    return;
                }
            }
            finally
            {
                _readLock.Release();
            }

            if (_readLockDisabled != null)
                await _readLockDisabled.Task.ConfigureAwait(false);

            goto Start;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No read operation is currently entered to exit from.
        /// </exception>
        public void ExitRead()
        {
            EnsureNotDisposed();

            try
            {
                _readLock.Wait();
                ExitReadCore();
            }
            finally
            {
                _readLock.Release();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No read operation is currently entered to exit from.
        /// </exception>
        public async Task ExitReadAsync()
        {
            EnsureNotDisposed();

            try
            {
                await _readLock.WaitAsync().ConfigureAwait(false);
                ExitReadCore();
            }
            finally
            {
                _readLock.Release();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// No read operation is currently entered to exit from.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExitReadCore()
        {
            if (ReaderCount == 0)
                throw new InvalidOperationException("No read operation is currently entered to exit from.");

            --ReaderCount;

            if (!CanRead && ReaderCount == 0)
            {
                _lastReaderExited?.TrySetResult(null);
                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public void EnterWrite()
        {
            EnsureNotDisposed();
            _writeLock.Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public Task EnterWriteAsync()
        {
            try
            {
                EnsureNotDisposed();
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }

            return _writeLock.WaitAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="SemaphoreFullException">
        /// No write operation is currently entered to exit from.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public void ExitWrite()
        {
            EnsureNotDisposed();
            _writeLock.Release();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public void EnableReadLock()
        {
            EnsureNotDisposed();

            Task waitTask = Task.CompletedTask;

            try
            {
                _readLock.Wait();
                waitTask = EnableReadLockCore();
            }
            finally
            {
                _readLock.Release();
            }

            waitTask.Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public async Task EnableReadLockAsync()
        {
            EnsureNotDisposed();

            Task waitTask;

            try
            {
                await _readLock.WaitAsync();
                waitTask = EnableReadLockCore();
            }
            finally
            {
                _readLock.Release();
            }

            await waitTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task EnableReadLockCore()
        {
            if (CanRead)
            {
                CanRead = false;
                _readLockDisabled = new TaskCompletionSource<object?>();

                if (ReaderCount > 0)
                {
                    _lastReaderExited = new TaskCompletionSource<object?>();
                    return _lastReaderExited.Task;
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public void DisableReadLock()
        {
            EnsureNotDisposed();

            try
            {
                _readLock.Wait();
                DisableReadLockCore();
            }
            finally
            {
                _readLock.Release();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The <see cref="ConsumerProducerLock"/> instance was disposed.
        /// </exception>
        public async Task DisableReadLockAsync()
        {
            EnsureNotDisposed();

            try
            {
                await _readLock.WaitAsync();
                DisableReadLockCore();
                
            }
            finally
            {
                _readLock.Release();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DisableReadLockCore()
        {
            CanRead = true;
            _readLockDisabled?.TrySetResult(null);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing">
        /// 
        /// </param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                _readLock.Dispose();
                _writeLock.Dispose();
            }
        }
    }
}

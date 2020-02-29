using System;
using System.Threading.Tasks;

namespace ConsumerProducerLocking
{
    /// <summary>
    /// TODO: COMMENT Write Session
    /// </summary>
    public class WriteSession : IDisposable, IAsyncDisposable
    {
        private volatile bool _disposed = false;
        private volatile bool _readLockEnabled = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lock">
        /// 
        /// </param>
        internal WriteSession(ConsumerProducerLock @lock)
        {
            Lock = @lock;
        }

        /// <summary>
        /// 
        /// </summary>
        ~WriteSession()
        {
            Disposing();
        }

        /// <summary>
        /// 
        /// </summary>
        public ConsumerProducerLock Lock { get; }

        /// <summary>
        /// 
        /// </summary>
        public void EnableReadLock()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (!_readLockEnabled)
            {
                _readLockEnabled = true;
                Lock.EnableReadLock();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        public Task EnableReadLockAsync()
        {
            try
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().FullName);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }

            if (!_readLockEnabled)
            {
                _readLockEnabled = true;
                return Lock.EnableReadLockAsync();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Disposing();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_readLockEnabled)
                await Lock.DisableReadLockAsync();

            Lock.ExitWrite();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Disposing()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_readLockEnabled)
                Lock.DisableReadLock();

            Lock.ExitWrite();
        }
    }
}

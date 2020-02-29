using System;
using System.Threading.Tasks;

namespace ConsumerProducerLocking
{
    /// <summary>
    /// TODO: COMMENT ReadSession
    /// </summary>
    public class ReadSession : IDisposable, IAsyncDisposable
    {
        private volatile bool _disposed = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lock">
        /// 
        /// </param>
        internal ReadSession(ConsumerProducerLock @lock)
        {
            Lock = @lock;
        }

        /// <summary>
        /// 
        /// </summary>
        ~ReadSession()
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
        public ValueTask DisposeAsync()
        {
            if (_disposed)
                return new ValueTask();

            _disposed = true;

            return new ValueTask(Lock.ExitReadAsync());
        }

        /// <summary>
        /// 
        /// </summary>
        private void Disposing()
        {
            if (_disposed)
                return;

            _disposed = true;

            Lock.ExitRead();
        }
    }
}

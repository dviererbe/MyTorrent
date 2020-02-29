using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ConsumerProducerLocking.Tests
{
    public class ConsumerProducerLockTests
    {
        private void AllMethods_Should_Throw_ObjectDisposedException(ConsumerProducerLock @lock)
        {
            Assert.Throws<ObjectDisposedException>(@lock.CreateReadSession);
            Assert.ThrowsAsync<ObjectDisposedException>(@lock.CreateReadSessionAsync);
            Assert.Throws<ObjectDisposedException>(@lock.CreateWriteSession);
            Assert.ThrowsAsync<ObjectDisposedException>(@lock.CreateWriteSessionAsync);
            Assert.Throws<ObjectDisposedException>(@lock.EnterRead);
            Assert.ThrowsAsync<ObjectDisposedException>(@lock.EnterReadAsync);
            Assert.Throws<ObjectDisposedException>(@lock.ExitRead);
            Assert.ThrowsAsync<ObjectDisposedException>(@lock.ExitReadAsync);
            Assert.Throws<ObjectDisposedException>(@lock.EnterWrite);
            Assert.ThrowsAsync<ObjectDisposedException>(@lock.EnterWriteAsync);
            Assert.Throws<ObjectDisposedException>(@lock.ExitWrite);
            Assert.Throws<ObjectDisposedException>(@lock.EnableReadLock);
            Assert.ThrowsAsync<ObjectDisposedException>(@lock.EnableReadLockAsync);
            Assert.Throws<ObjectDisposedException>(@lock.DisableReadLock);
            Assert.ThrowsAsync<ObjectDisposedException>(@lock.DisableReadLockAsync);
        }

        #region ConsumerProducerLock.ctor() Tests

        [Fact]
        public void Constructor_Should_CreateEmptyInstance()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();

            Assert.True(@lock.CanRead);
            Assert.Equal(0, @lock.ReaderCount);
        }

        #endregion

        #region ConsumerProducerLock.CreateReadSession() Tests

        [Fact]
        public void CreateReadSession_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.CreateReadSession);
        }

        #endregion

        #region ConsumerProducerLock.CreateReadSessionAsync() Tests

        [Fact]
        public void CreateReadSessionAsync_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(@lock.CreateReadSessionAsync);
        }

        #endregion

        #region ConsumerProducerLock.CreateWriteSession() Tests

        [Fact]
        public void CreateWriteSession_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.CreateWriteSession);
        }

        #endregion

        #region ConsumerProducerLock.CreateWriteSessionAsync() Tests

        [Fact]
        public void CreateWriteSessionAsync_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(@lock.CreateWriteSessionAsync);
        }

        #endregion

        #region ConsumerProducerLock.EnterRead() Tests

        [Fact]
        public void EnterRead_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.EnterRead);
        }

        #endregion

        #region ConsumerProducerLock.EnterReadAsync() Tests

        [Fact]
        public void EnterReadAsync_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(@lock.EnterReadAsync);
        }

        #endregion

        #region ConsumerProducerLock.ExitRead() Tests

        [Fact]
        public void ExitRead_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.ExitRead);
        }

        #endregion

        #region ConsumerProducerLock.ExitReadAsync() Tests

        [Fact]
        public void ExitReadAsync_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(@lock.ExitReadAsync);
        }

        #endregion

        #region ConsumerProducerLock.EnterWrite() Tests

        [Fact]
        public void EnterWrite_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.EnterWrite);
        }

        #endregion

        #region ConsumerProducerLock.EnterWriteAsync() Tests

        [Fact]
        public void EnterWriteAsync_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(@lock.EnterWriteAsync);
        }

        #endregion

        #region ConsumerProducerLock.ExitWrite() Tests

        [Fact]
        public void ExitWrite_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.ExitWrite);
        }

        #endregion

        #region ConsumerProducerLock.EnableReadLock() Tests

        [Fact]
        public void EnableReadLock_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.EnableReadLock);
        }

        #endregion

        #region ConsumerProducerLock.EnableReadLockAsync() Tests

        [Fact]
        public void EnableReadLockAsync_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(@lock.EnableReadLockAsync);
        }

        #endregion

        #region ConsumerProducerLock.DisableReadLock() Tests

        [Fact]
        public void DisableReadLock_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.Throws<ObjectDisposedException>(@lock.DisableReadLock);
        }

        #endregion

        #region ConsumerProducerLock.DisableReadLockAsync() Tests

        [Fact]
        public void DisableReadLockAsync_Should_Throw_ObjectDisposed_When_InstanceWasDisposed()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();
            @lock.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(@lock.DisableReadLockAsync);
        }

        #endregion

        #region ConsumerProducerLock.Dispose() Tests

        [Fact]
        public void Dispose_Should_DisposeInstance()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();

            @lock.Dispose();

            AllMethods_Should_Throw_ObjectDisposedException(@lock);
        }

        [Fact]
        public void Dispose_Should_BeIdempotent()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();

            @lock.Dispose();

            AllMethods_Should_Throw_ObjectDisposedException(@lock);

            @lock.Dispose();

            AllMethods_Should_Throw_ObjectDisposedException(@lock);

            @lock.Dispose();

            AllMethods_Should_Throw_ObjectDisposedException(@lock);

            @lock.Dispose();

            AllMethods_Should_Throw_ObjectDisposedException(@lock);

            @lock.Dispose();

            AllMethods_Should_Throw_ObjectDisposedException(@lock);
        }

        #endregion

        //Here are the combination ob multiple methods tested.
        #region Integration Tests

        [Fact]
        public void IntegrationTest1()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();

            Assert.True(@lock.CanRead);
            Assert.Equal(0, @lock.ReaderCount);

            @lock.EnterRead();

            Assert.True(@lock.CanRead);
            Assert.Equal(1, @lock.ReaderCount);

            @lock.EnterRead();

            Assert.True(@lock.CanRead);
            Assert.Equal(2, @lock.ReaderCount);

            @lock.ExitRead();

            Assert.True(@lock.CanRead);
            Assert.Equal(1, @lock.ReaderCount);

            @lock.ExitRead();

            Assert.True(@lock.CanRead);
            Assert.Equal(0, @lock.ReaderCount);
        }

        [Fact]
        public void IntegrationTest2()
        {
            ConsumerProducerLock @lock = new ConsumerProducerLock();

            Assert.True(@lock.CanRead);
            Assert.Equal(0, @lock.ReaderCount);

            using (ReadSession readSession = @lock.CreateReadSession())
            {
                Assert.True(@lock.CanRead);
                Assert.Equal(1, @lock.ReaderCount);
            }

            Assert.True(@lock.CanRead);
            Assert.Equal(0, @lock.ReaderCount);
        }

        #endregion

    }
}

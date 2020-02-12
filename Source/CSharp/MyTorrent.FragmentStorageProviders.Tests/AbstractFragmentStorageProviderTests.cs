using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using MyTorrent.HashingServiceProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace MyTorrent.FragmentStorageProviders.Tests
{
    public abstract class AbstractFragmentStorageProviderTests
    {
        private static (string Hash, byte[] Data) _ExampleFragment = ("554133128EB3105B9BAD661B8D3D118BCBD9D1568DD2FB70087F89041B0E03B0", new byte[] { 0x48, 0x61, 0x6c, 0x6c, 0x6f, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21 });

        public AbstractFragmentStorageProviderTests()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            CanceledCancellationToken = cancellationTokenSource.Token;

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(loggerBuilder =>
            {
                loggerBuilder.AddDebug();
            });

            IOptions<StandardHashingServiceProviderOptions> options = Options.Create<StandardHashingServiceProviderOptions>(new StandardHashingServiceProviderOptions()
            {
                HashAlgorithm = "SHA256"
            });

            HashingServiceProvider = new StandardHashingServiceProvider(options);
        }
        
        public CancellationToken CanceledCancellationToken { get; }

        protected virtual string InvalidHashValue => "ABC";

        protected virtual ref readonly (string Hash, byte[] Data) ExampleFragment => ref _ExampleFragment;

        protected virtual IHashingServiceProvider HashingServiceProvider { get; }

        protected virtual ILoggerFactory LoggerFactory { get; }

        protected ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        protected abstract IFragmentStorageProvider CreateNewEmptyStorageProvider();
        
        protected static void ThrowsNoObjectDisposedException(Action testCode)
        {
            try
            {
                testCode();
            }
            catch (Exception exception)
            {
                Assert.IsNotType<ObjectDisposedException>(exception);
            }
        }

        protected static void ThrowsNoObjectDisposedException(Func<object> testCode)
        {
            try
            {
                testCode();
            }
            catch (Exception exception)
            {
                Assert.IsNotType<ObjectDisposedException>(exception);
            }
        }

        protected static void ThrowsNoObjectDisposedExceptionAsync(Func<Task> testCode)
        {
            try
            {
                testCode().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Assert.IsNotType<ObjectDisposedException>(exception);
            }
        }

        protected virtual void CheckStorageProviderIsNotDisposed(IFragmentStorageProvider fragmentStorageProvider)
        {
            Assert.False(fragmentStorageProvider.Disposed);
            
            ThrowsNoObjectDisposedException(() => fragmentStorageProvider.AvailableStorageSpace);
            ThrowsNoObjectDisposedException(() => fragmentStorageProvider.UsedStorageSpace);
            ThrowsNoObjectDisposedException(() => fragmentStorageProvider.StorageSpaceUsageLimit);
            ThrowsNoObjectDisposedException(() => fragmentStorageProvider.Fragments);
            ThrowsNoObjectDisposedException(() => fragmentStorageProvider.Allocations);

            ThrowsNoObjectDisposedException(() =>
            { 
                Stream stream = fragmentStorageProvider.ReadFragment(ExampleFragment.Hash, delete: false);
                stream.Dispose();
                stream.Dispose(); //Check if Dispose is idempotent.
            });

            ThrowsNoObjectDisposedException(() =>
            {
                Stream stream = fragmentStorageProvider.WriteFragment(ExampleFragment.Hash, ExampleFragment.Data.LongLength);
                stream.Dispose();
                stream.Dispose(); //Check if Dispose is idempotent.
            });

            ThrowsNoObjectDisposedExceptionAsync(() => fragmentStorageProvider
                                                            .AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength)
                                                            .ContinueWith(task => task.Result.Dispose()));

            ThrowsNoObjectDisposedExceptionAsync(async () => await fragmentStorageProvider.IsFragmentStoredAsync(ExampleFragment.Hash));

            ThrowsNoObjectDisposedExceptionAsync(() => fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: false));
            ThrowsNoObjectDisposedExceptionAsync(() => fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: true));
            ThrowsNoObjectDisposedExceptionAsync(() => fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data));
            ThrowsNoObjectDisposedExceptionAsync(() => fragmentStorageProvider.DeleteFragmentAsync(ExampleFragment.Hash, wait: false));
            ThrowsNoObjectDisposedExceptionAsync(() => fragmentStorageProvider.DeleteFragmentAsync(ExampleFragment.Hash, wait: true));
        }

        protected virtual void CheckStorageProviderIsDisposed(IFragmentStorageProvider fragmentStorageProvider)
        {
            Assert.True(fragmentStorageProvider.Disposed);

            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.AvailableStorageSpace);
            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.UsedStorageSpace);
            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.StorageSpaceUsageLimit);
            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.Fragments);
            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.Allocations);

            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.ReadFragment(ExampleFragment.Hash, delete: false));
            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.ReadFragment(ExampleFragment.Hash, delete: true));
            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.WriteFragment(ExampleFragment.Hash, ExampleFragment.Data.LongLength));

            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength));
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await fragmentStorageProvider.IsFragmentStoredAsync(ExampleFragment.Hash));
            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: false));
            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: true));
            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data));
            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.DeleteFragmentAsync(ExampleFragment.Hash, wait: false));
            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.DeleteFragmentAsync(ExampleFragment.Hash, wait: true));
        }

        protected virtual void CheckStorageAllocationTokenIsDisposed(IStorageSpaceAllocationToken allocationToken)
        {
            Assert.True(allocationToken.Disposed);

            Assert.Throws<ObjectDisposedException>(() => allocationToken.AvailableFreeSpace);
            Assert.Throws<ObjectDisposedException>(() => allocationToken.UsedSpace);
            Assert.Throws<ObjectDisposedException>(() => allocationToken.Fragments);
            Assert.Throws<ObjectDisposedException>(() => allocationToken.PersistentFragments);
            
            Assert.ThrowsAsync<ObjectDisposedException>(() => allocationToken.CommitAsync());
        }

        protected virtual async Task CheckStorageProviderIsEmptyAsync(IFragmentStorageProvider fragmentStorageProvider)
        {
            Assert.Empty(fragmentStorageProvider.Fragments);
            Assert.Equal(0, fragmentStorageProvider.Fragments.Count);
            
            Assert.False(await fragmentStorageProvider.IsFragmentStoredAsync(ExampleFragment.Hash));

            long usedStorageSpace = 0L;

            foreach (var allocationToken in fragmentStorageProvider.Allocations)
            {
                Assert.Equal(0L, allocationToken.UsedSpace);
                Assert.Empty(allocationToken.Fragments);
                Assert.Empty(allocationToken.PersistentFragments);

                usedStorageSpace += allocationToken.TotalAllocatedStorageSpace;
            }

            Assert.Equal(fragmentStorageProvider.UsedStorageSpace, usedStorageSpace);
        }

        protected virtual async Task CheckStorageProviderContainsJustExampleFragmentAsync(IFragmentStorageProvider fragmentStorageProvider)
        {
            Assert.NotEmpty(fragmentStorageProvider.Fragments);
            Assert.Contains(ExampleFragment.Hash, fragmentStorageProvider.Fragments);
            Assert.Equal(1, fragmentStorageProvider.Fragments.Count);
            Assert.NotEqual(0, fragmentStorageProvider.UsedStorageSpace);

            Assert.True(await fragmentStorageProvider.IsFragmentStoredAsync(ExampleFragment.Hash));

            bool found = false;

            foreach (var allocationToken in fragmentStorageProvider.Allocations)
            {
                if (!found)
                {
                    int count = allocationToken.Fragments.Count();

                    if (count != 0)
                    {
                        Assert.Equal(1, count);
                        Assert.NotEqual(0L, allocationToken.UsedSpace);
                        Assert.NotEmpty(allocationToken.Fragments);

                        Assert.Equal(ExampleFragment.Hash, allocationToken.Fragments.Single());

                        found = true;
                        continue;
                    }
                }

                Assert.Equal(0L, allocationToken.UsedSpace);
                Assert.Equal(allocationToken.TotalAllocatedStorageSpace, allocationToken.AvailableFreeSpace);
                Assert.Empty(allocationToken.Fragments);
                Assert.Empty(allocationToken.PersistentFragments);
            }
        }

        [Fact]
        public async Task Should_BeEmpty_When_NewlyCreated()
        {
            using IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            await CheckStorageProviderIsEmptyAsync(fragmentStorageProvider);
        }

        #region Dispose Tests

        [Fact]
        public virtual void Dispose_Should_DisposeStorageProvider()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            CheckStorageProviderIsNotDisposed(fragmentStorageProvider);
            fragmentStorageProvider.Dispose();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        #endregion

        #region DisposeAsync Tests

        [Fact]
        public virtual async Task DisposeAsync_Should_DisposeStorageProvider()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            CheckStorageProviderIsNotDisposed(fragmentStorageProvider);
            await fragmentStorageProvider.DisposeAsync();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        #endregion

        #region AllocateStorageSpaceAsync Tests

        [Fact]
        public void AllocateStorageSpaceAsync_Should_Throw_ArgumentOutOfRangeException_WhenSizeIsNegative()
        {
            using IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            for (int size = -1; size > -100; --size)
            {
                Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fragmentStorageProvider.AllocateStorageSpaceAsync(size));
            }
        }

        #endregion

        #region IsFragmentStoredAsync Tests

        #endregion

        #region GetFragmentAsync Tests

        [Fact]
        public async Task GetFragmentAsync_Should_ReturnStoredFragment()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            await fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data);

            byte[] data = await fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: false);

            Assert.Equal(ExampleFragment.Data, data);
            await CheckStorageProviderContainsJustExampleFragmentAsync(fragmentStorageProvider);

            await fragmentStorageProvider.DisposeAsync();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        [Fact]
        public async Task GetFragmentAsync_Should_ReturnStoredFragmentAndDeleteIt()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            await fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data);

            byte[] data = await fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: true);

            Assert.Equal(ExampleFragment.Data, data);
            await CheckStorageProviderIsEmptyAsync(fragmentStorageProvider);

            await fragmentStorageProvider.DisposeAsync();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        [Fact]
        public async Task GetFragmentAsync_Should_ReturnStoredFragment_When_StoredWithAllocationToken()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            IStorageSpaceAllocationToken allocationToken = await fragmentStorageProvider.AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength);
            await fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data, allocationToken);

            byte[] data = await fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: false);

            Assert.Equal(ExampleFragment.Data, data);
            await CheckStorageProviderContainsJustExampleFragmentAsync(fragmentStorageProvider);

            await allocationToken.DisposeAsync();
            CheckStorageAllocationTokenIsDisposed(allocationToken);
            await CheckStorageProviderIsEmptyAsync(fragmentStorageProvider);

            await fragmentStorageProvider.DisposeAsync();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        [Fact]
        public async Task GetFragmentAsync_Should_ReturnStoredFragmentAndDeleteIt_When_StoredWithAllocationToken()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            IStorageSpaceAllocationToken allocationToken = await fragmentStorageProvider.AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength);
            await fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data, allocationToken);

            byte[] data = await fragmentStorageProvider.GetFragmentAsync(ExampleFragment.Hash, delete: true);

            Assert.Equal(ExampleFragment.Data, data);
            await CheckStorageProviderIsEmptyAsync(fragmentStorageProvider);

            await allocationToken.DisposeAsync();
            CheckStorageAllocationTokenIsDisposed(allocationToken);
            await CheckStorageProviderIsEmptyAsync(fragmentStorageProvider);

            await fragmentStorageProvider.DisposeAsync();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        #endregion

        #region ReadFragment Tests

        [Fact]
        public void ReadFragment_Should_Throw_ArgumentNullException_WhenFaragmentHashIsNull()
        {
            using IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            Assert.Throws<ArgumentNullException>(() => fragmentStorageProvider.ReadFragment(null, delete: false));
            Assert.Throws<ArgumentNullException>(() => fragmentStorageProvider.ReadFragment(null, delete: true));
        }

        [Fact]
        public void ReadFragment_Should_Throw_FormatException_WhenFaragmentHashIsInvalid()
        {
            using IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            Assert.Throws<FormatException>(() => fragmentStorageProvider.ReadFragment(InvalidHashValue, delete: false));
            Assert.Throws<FormatException>(() => fragmentStorageProvider.ReadFragment(InvalidHashValue, delete: true));
        }

        [Fact]
        public void ReadFragment_Should_Throw_KeyNotFoundException_WhenStorageProviderContainsNoFragmentWithSpecifiedFragmentHash()
        {
            using IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            Assert.Throws<KeyNotFoundException>(() => fragmentStorageProvider.ReadFragment(ExampleFragment.Hash, delete: false));
            Assert.Throws<KeyNotFoundException>(() => fragmentStorageProvider.ReadFragment(ExampleFragment.Hash, delete: true));
        }

        [Fact]
        public void ReadFragment_Should_Throw_ObjectDisposedException_WhenStorageProviderWasDisposed()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            fragmentStorageProvider.Dispose();

            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.ReadFragment(ExampleFragment.Hash, delete: false));
            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.ReadFragment(ExampleFragment.Hash, delete: true));
        }

        #endregion

        #region StoreFragmentAsync Tests

        [Fact]
        public async Task StoreFragmentAsync_Should_StoreExampleFragment()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            await fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data);
            await CheckStorageProviderContainsJustExampleFragmentAsync(fragmentStorageProvider);

            await fragmentStorageProvider.DisposeAsync();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        [Fact]
        public async Task StoreFragmentAsync_Should_StoreExampleFragmentWithAllocationToken()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            IStorageSpaceAllocationToken allocationToken = await fragmentStorageProvider.AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength);

            await fragmentStorageProvider.StoreFragmentAsync(ExampleFragment.Hash, ExampleFragment.Data, allocationToken);
            await CheckStorageProviderContainsJustExampleFragmentAsync(fragmentStorageProvider);

            await allocationToken.DisposeAsync();
            CheckStorageAllocationTokenIsDisposed(allocationToken);

            await fragmentStorageProvider.DisposeAsync();
            CheckStorageProviderIsDisposed(fragmentStorageProvider);
        }

        #endregion

        #region WriteFragment Tests

        [Fact]
        public async Task WriteFragment_Should_Throw_ArgumentNullException_WhenFaragmentHashIsNull()
        {
            using (IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider())
            {
                Assert.Throws<ArgumentNullException>(() => fragmentStorageProvider.WriteFragment(null, ExampleFragment.Data.LongLength));
            }

            using (IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider())
            {
                using IStorageSpaceAllocationToken allocationToken = await fragmentStorageProvider.AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength);

                Assert.Throws<ArgumentNullException>(() => fragmentStorageProvider.WriteFragment(null, ExampleFragment.Data.LongLength));
            }
        }

        [Fact]
        public async Task WriteFragment_Should_Throw_FormatException_WhenFaragmentHashIsInvalid()
        {
            using (IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider())
            {
                Assert.Throws<FormatException>(() => fragmentStorageProvider.WriteFragment(InvalidHashValue, ExampleFragment.Data.LongLength));
            }

            using (IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider())
            {
                using IStorageSpaceAllocationToken allocationToken = await fragmentStorageProvider.AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength);

                Assert.Throws<FormatException>(() => fragmentStorageProvider.WriteFragment(InvalidHashValue, ExampleFragment.Data.LongLength, allocationToken));
            }
        }

        [Fact]
        public async Task WriteFragment_Should_Throw_ArgumentOutOfRangeException_WhenSizeIsNegative()
        {
            using (IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider())
            {
                for (int size = -1; size > -100; --size)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => fragmentStorageProvider.WriteFragment(ExampleFragment.Hash, size));
                }
            }

            using (IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider())
            {
                using IStorageSpaceAllocationToken allocationToken = await fragmentStorageProvider.AllocateStorageSpaceAsync(ExampleFragment.Data.LongLength);

                for (int size = -1; size > -100; --size)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => fragmentStorageProvider.WriteFragment(ExampleFragment.Hash, size, allocationToken));
                }
            }
        }

        [Fact]
        public void WriteFragment_Should_Throw_ObjectDisposedException_WhenStorageProviderWasDisposed()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            fragmentStorageProvider.Dispose();

            Assert.Throws<ObjectDisposedException>(() => fragmentStorageProvider.WriteFragment(ExampleFragment.Hash, ExampleFragment.Data.LongLength));
        }
        
        #endregion

        #region DeleteFragmentAsync Tests

        [Fact]
        public void DeleteFragmentAsync_Should_Throw_ArgumentNullException_WhenFaragmentHashIsNull()
        {
            using IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            Assert.ThrowsAsync<ArgumentNullException>(() => fragmentStorageProvider.DeleteFragmentAsync(null, true));
            Assert.ThrowsAsync<ArgumentNullException>(() => fragmentStorageProvider.DeleteFragmentAsync(null, false));
        }

        [Fact]
        public void DeleteFragmentAsync_Should_Throw_FormatException_WhenFaragmentHashIsInvalid()
        {
            using IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();

            Assert.ThrowsAsync<FormatException>(() => fragmentStorageProvider.DeleteFragmentAsync(InvalidHashValue, true));
            Assert.ThrowsAsync<FormatException>(() => fragmentStorageProvider.DeleteFragmentAsync(InvalidHashValue, false));
        }

        [Fact]
        public void DeleteFragmentAsync_Should_Throw_ObjectDisposedException_WhenStorageProviderWasDisposed()
        {
            IFragmentStorageProvider fragmentStorageProvider = CreateNewEmptyStorageProvider();
            fragmentStorageProvider.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.DeleteFragmentAsync(ExampleFragment.Hash, wait: true));
            Assert.ThrowsAsync<ObjectDisposedException>(() => fragmentStorageProvider.DeleteFragmentAsync(ExampleFragment.Hash, wait: false));
        }
        
        #endregion
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MyTorrent.FragmentStorageProviders.Tests
{
    public class VirtualManagedFragmentFileStorageProviderTests : AbstractFragmentStorageProviderTests, IDisposable
    {
        private const string TestFolderPath = "TESTS (CAN BE DELETED)";

        private List<VirtualManagedFragmentFileStorageProvider> _storageProviders;

        public VirtualManagedFragmentFileStorageProviderTests()
        {
            _storageProviders = new List<VirtualManagedFragmentFileStorageProvider>();

            DeleteTestFolder();
        }

        private void DeleteTestFolder()
        {
            DirectoryInfo directory = new DirectoryInfo(TestFolderPath);

            if (directory.Exists)
                directory.Delete(recursive: true);
        }

        protected override IFragmentStorageProvider CreateNewEmptyStorageProvider()
        {
            string storageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), TestFolderPath, Guid.NewGuid().ToString("B"));

            IOptions<FragmentFileStorageProviderOptions> options =
                Options.Create(new FragmentFileStorageProviderOptions(storageFolderPath, 1024, true));

            var storageProvider =  new VirtualManagedFragmentFileStorageProvider(
                logger: CreateLogger<VirtualManagedFragmentFileStorageProvider>(),
                eventIdCreationSource: new EventIdCreationSource(),
                HashingServiceProvider,
                options);

            _storageProviders.Add(storageProvider);

            return storageProvider;
        }

        public void Dispose()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30);
            DateTime started = DateTime.Now;

            try
            {
                while (_storageProviders.Count != 0)
                {
                    for (int i = _storageProviders.Count - 1; i >= 0; --i)
                    {
                        var storageProvider = _storageProviders[i];

                        if (storageProvider.Disposed)
                            _storageProviders.RemoveAt(i);
                    }

                    if (DateTime.Now - started > timeout)
                        throw new TimeoutException();
                }
            }
            finally
            {
                foreach (var storageProvider in _storageProviders)
                {
                    storageProvider.Dispose();
                }

                DeleteTestFolder();
            }
        }
    }
}

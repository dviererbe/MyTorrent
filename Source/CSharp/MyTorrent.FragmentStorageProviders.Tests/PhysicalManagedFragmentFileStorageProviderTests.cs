using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyTorrent.FragmentStorageProviders.Tests
{
    class PhysicalManagedFragmentFileStorageProviderTests : AbstractFragmentStorageProviderTests, IDisposable
    {
        private const string TestFolderPath = "TESTS (CAN BE DELETED)/Physical";

        private List<PhysicalManagedFragmentFileStorageProvider> _storageProviders;

        public PhysicalManagedFragmentFileStorageProviderTests()
        {
            _storageProviders = new List<PhysicalManagedFragmentFileStorageProvider>();

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

            IOptions<PhysicalManagedFragmentFileStorageProviderOptions> options =
                Options.Create(new PhysicalManagedFragmentFileStorageProviderOptions(storageFolderPath, 1024, true));

            var storageProvider = new PhysicalManagedFragmentFileStorageProvider(
                logger: CreateLogger<PhysicalManagedFragmentFileStorageProvider>(),
                eventIdCreationSource: new EventIdCreationSource(),
                HashingServiceProvider,
                options);

            _storageProviders.Add(storageProvider);

            return storageProvider;
        }

        public void Dispose()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(5);
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

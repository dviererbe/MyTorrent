using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTorrent.FragmentStorageProviders;

namespace MyTorrent.FragmentStorageProviders.Tests
{
    public class FragmentInMemoryStorageProviderTests : AbstractFragmentStorageProviderTests
    {
        protected override IFragmentStorageProvider CreateNewEmptyStorageProvider()
        {
            IOptions<FragmentInMemoryStorageProviderOptions> options = 
                Options.Create(FragmentInMemoryStorageProviderOptions.Default);

            return new FragmentInMemoryStorageProvider(
                logger: CreateLogger<FragmentInMemoryStorageProvider>(),
                eventIdCreationSource: new EventIdCreationSource(),
                HashingServiceProvider,
                options);
        }
    }
}

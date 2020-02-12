using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyTorrent.FragmentStorageProviders.Tests
{
    public class PhysicalManagedFragmentFileStorageProviderTests : AbstractFragmentStorageProviderTests
    {
        protected override IFragmentStorageProvider CreateNewEmptyStorageProvider()
        {
            throw new NotImplementedException();
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    public partial class FragmentInMemoryStorageProvider
    {
        //Just a POD (Plain old data structure)
        internal class InMemoryFragment
        {
            internal InMemoryFragment(byte[] data, bool persistent, InMemoryStorageSpaceAllocationToken? allocationToken = null)
            {
                Data = data;
                Remove = false;
                Persistent = persistent;
                AllocationToken = allocationToken;
                FragmentRemovalTaskSource = null;
                ReadOperations = new List<FragmentInMemoryReadStream>();
            }

            public byte[] Data;
            public bool Remove;
            public bool Persistent;
            public List<FragmentInMemoryReadStream> ReadOperations;
            public InMemoryStorageSpaceAllocationToken? AllocationToken;
            public TaskCompletionSource<object?>? FragmentRemovalTaskSource;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    partial class VirtualManagedFragmentFileStorageProvider
    {
        //Just a POD (Plain old data structure)
        internal class FragmentMetadata
        {
            internal FragmentMetadata(string normalizedFragmentHash, long size, bool persistent,  VirtualManagedFileStorageSpaceAllocationToken? allocationToken = null)
            {
                NormalizedFragmentHash = normalizedFragmentHash;

                Size = size;
                Remove = false;
                Persistent = persistent;
                AllocationToken = allocationToken;
                ReadOperations = new List<VirtualManagedFragmentFileReadStream>();
            }

            internal string NormalizedFragmentHash;

            internal long Size;
            internal bool Remove;
            internal bool Persistent;
            internal List<VirtualManagedFragmentFileReadStream> ReadOperations;
            internal VirtualManagedFileStorageSpaceAllocationToken? AllocationToken;
            internal TaskCompletionSource<object?>? FragmentRemovalTaskSource;
        }
    }
}

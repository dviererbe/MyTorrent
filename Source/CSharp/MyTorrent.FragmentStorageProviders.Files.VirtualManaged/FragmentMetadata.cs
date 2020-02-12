using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    partial class VirtualManagedFragmentFileStorageProvider
    {
        /// <summary>
        /// A Plain old data structure to store information about a fragment.
        /// </summary>
        internal class FragmentMetadata
        {
            /// <summary>
            /// Initializes a new <see cref="FragmentMetadata"/> instance.
            /// </summary>
            /// <param name="normalizedFragmentHash">
            /// Normalized hash value of the fragment.
            /// </param>
            /// <param name="size">
            /// Length of the content in bytes.
            /// </param>
            /// <param name="persistent">
            /// <see langword="true"/> if the fragment should not be removed when the 
            /// coresponding allocation token will be disposed -or- <paramref name="allocationToken"/> 
            /// is null; otherwise <see langword="false"/>.
            /// </param>
            /// <param name="allocationToken">
            /// The allocation token that is associated to this fragment.
            /// </param>
            internal FragmentMetadata(string normalizedFragmentHash, long size, bool persistent,  VirtualManagedFileStorageSpaceAllocationToken? allocationToken = null)
            {
                NormalizedFragmentHash = normalizedFragmentHash;

                Size = size;
                Remove = false;
                Persistent = persistent;
                AllocationToken = allocationToken;
                ReadOperations = new List<VirtualManagedFragmentFileReadStream>();
            }

            /// <summary>
            /// Normalized hash value of the fragment content.
            /// </summary>
            internal string NormalizedFragmentHash;

            /// <summary>
            /// Length of the fragment content in bytes.
            /// </summary>
            internal long Size;

            /// <summary>
            /// <see langword="true"/> if the fragment should be removed when the last reader 
            /// finished reading; otherwise <see langword="false"/>.
            /// </summary>
            internal bool Remove;

            /// <summary>
            /// <see langword="true"/> if the fragment should persist on the filesystem even after the
            /// storage provider stoped running; also <see langword="true"/> if the fragment should not be removed when the coresponding 
            /// allocation token will be disposed; otherwise <see langword="false"/>.
            /// </summary>
            internal bool Persistent;

            /// <summary>
            /// Active streams that read the fragments content.
            /// </summary>
            internal List<VirtualManagedFragmentFileReadStream> ReadOperations;

            /// <summary>
            /// The allocation token that is associated to this fragment.
            /// </summary>
            internal VirtualManagedFileStorageSpaceAllocationToken? AllocationToken;

            /// <summary>
            /// The task completion source that should be set as completed when the last reader finished 
            /// reading, this is not <see langword="null"/> and <see cref="Remove"/> is <see langword="true"/>.
            /// </summary>
            internal TaskCompletionSource<object?>? FragmentRemovalTaskSource;
        }
    }
}

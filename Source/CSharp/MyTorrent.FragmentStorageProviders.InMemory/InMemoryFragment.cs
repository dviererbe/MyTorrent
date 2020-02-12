using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTorrent.FragmentStorageProviders
{
    public partial class FragmentInMemoryStorageProvider
    {
        /// <summary>
        /// A Plain old data structure to store information about a fragment in memory.
        /// </summary>
        internal class InMemoryFragment
        {
            /// <summary>
            /// Initializes a new <see cref="InMemoryFragment"/> instance.
            /// </summary>
            /// <param name="data">
            /// Content of the fragment.
            /// </param>
            /// <param name="persistent">
            /// <see langword="true"/> if the fragment should not be removed when the 
            /// coresponding allocation token will be disposed -or- <paramref name="allocationToken"/> 
            /// is null; otherwise <see langword="false"/>.
            /// </param>
            /// <param name="allocationToken">
            /// The allocation token that is associated to this fragment.
            /// </param>
            internal InMemoryFragment(byte[] data, bool persistent, InMemoryStorageSpaceAllocationToken? allocationToken = null)
            {
                Data = data;
                Remove = false;
                Persistent = persistent;
                AllocationToken = allocationToken;
                FragmentRemovalTaskSource = null;
                ReadOperations = new List<FragmentInMemoryReadStream>();
            }

            /// <summary>
            /// Content of the fragment.
            /// </summary>
            public byte[] Data;

            /// <summary>
            /// <see langword="true"/> if the fragment should be removed when the last reader 
            /// finished reading; otherwise <see langword="false"/>.
            /// </summary>
            public bool Remove;

            /// <summary>
            /// <see langword="true"/> if the fragment should not be removed when the coresponding 
            /// allocation token will be disposed; otherwise <see langword="false"/>.
            /// </summary>
            public bool Persistent;

            /// <summary>
            /// Active streams that read the fragments content.
            /// </summary>
            public List<FragmentInMemoryReadStream> ReadOperations;

            /// <summary>
            /// The allocation token that is associated to this fragment.
            /// </summary>
            public InMemoryStorageSpaceAllocationToken? AllocationToken;

            /// <summary>
            /// The task completion source that should be set as completed when the last reader finished 
            /// reading, this is not <see langword="null"/> and <see cref="Remove"/> is <see langword="true"/>.
            /// </summary>
            public TaskCompletionSource<object?>? FragmentRemovalTaskSource;
        }
    }
}

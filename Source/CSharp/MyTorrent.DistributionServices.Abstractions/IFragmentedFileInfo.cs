using System;
using System.Collections.Generic;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Holds information about a file and fragments it consists of.
    /// </summary>
    public interface IFragmentedFileInfo
    {
        /// <summary>
        /// Gets the hash value of the entire file content.
        /// </summary>
        public string Hash { get; }

        /// <summary>
        /// Gets the size of the entire file content in byte.
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// Gets the sequence of hash values of the fragments the file consists of. 
        /// </summary>
        /// <remarks>
        /// The order of the hash values is relevant because it indicates how the fragmented file must be assembled.
        /// </remarks>
        public IEnumerable<string> FragmentSequence { get; }
    }
}

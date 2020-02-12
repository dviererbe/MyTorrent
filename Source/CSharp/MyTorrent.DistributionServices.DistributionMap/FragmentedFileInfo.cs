using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Holds information about a file and fragments it consists of.
    /// Default implementation of <see cref="IFragmentedFileInfo"/>
    /// </summary>
    public class FragmentedFileInfo : IFragmentedFileInfo, IEquatable<IFragmentedFileInfo>, IEquatable<FragmentedFileInfo>
    {
        private readonly int HashCode;

        /// <summary>
        /// Initializes a new instance of <see cref="FragmentedFileInfo"/>.
        /// </summary>
        /// <param name="fileHash">
        /// Hash value of the entire file content.
        /// </param>
        /// <param name="fileSize">
        /// Size of the entire file content in byte.
        /// </param>
        /// <param name="fragmentSequence">
        /// Sequence of hash values of the fragments the file consists of. 
        /// The order of the hash values is relevant because it indicates how the fragmented file must be assembled.
        /// </param>
        public FragmentedFileInfo(string fileHash, long fileSize, IEnumerable<string> fragmentSequence)
        {
            Hash = fileHash;
            Size = fileSize;
            FragmentSequence = fragmentSequence;

            #region CalculateHashCode
            
            HashCode = 13;

            unchecked
            {
                HashCode = (HashCode * 7) + Hash.GetHashCode();
                HashCode = (HashCode * 7) + Size.GetHashCode();

                int i = 0;

                foreach (string fragmentHash in fragmentSequence)
                {
                    HashCode = (HashCode * 7) + Size.GetHashCode() + i++;
                }
            }
            
            #endregion
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is IFragmentedFileInfo fileInfo)
                return EqualsCore(this, fileInfo);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FragmentedFileInfo other)
        {
            if (other == null)
                return false;

            return EqualsCore(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IFragmentedFileInfo other)
        {
            if (other == null)
                return false;

            return EqualsCore(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsCore(IFragmentedFileInfo fileInfo1, IFragmentedFileInfo fileInfo2)
        {
            return fileInfo1.Hash.Equals(fileInfo2.Hash) 
                && fileInfo1.Size == fileInfo2.Size 
                && fileInfo1.FragmentSequence.SequenceEqual(fileInfo2.FragmentSequence);            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FragmentedFileInfo fileInfo1, FragmentedFileInfo fileInfo2)
        {
            if (fileInfo1 == null)
                return fileInfo2 == null;
            
            if (fileInfo2 == null)
                return fileInfo1 == null;

            return EqualsCore(fileInfo1, fileInfo2);
        }

        public static bool operator !=(FragmentedFileInfo fileInfo1, FragmentedFileInfo fileInfo2)
        {
            if (fileInfo1 == null)
                return fileInfo2 != null;

            if (fileInfo2 == null)
                return fileInfo1 != null;

            return !EqualsCore(fileInfo1, fileInfo2);
        }
    }
}

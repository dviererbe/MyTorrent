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

        /// <summary>
        /// Gets a hash code for the current object.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode;
        }

        /// <summary>
        /// Determines whether the current object and another <see cref="object"/> instance are equal.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> instance to compare with the current object.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified object is equal to the current object; otherwise, <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is IFragmentedFileInfo fileInfo)
                return EqualsCore(this, fileInfo);

            return false;
        }

        /// <summary>
        /// Determines whether the current object and another <see cref="FragmentedFileInfo"/> instance are equal.
        /// </summary>
        /// <param name="other">
        /// The <see cref="FragmentedFileInfo"/> instance to compare with the current object.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified instance is equal to the current object; otherwise, <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FragmentedFileInfo other)
        {
            //Paremeter "other" can theoretical not be null, because in the project is Nullable enabled, but
            //projects, where nullable is not enabled can still call this function with null values.
#pragma warning disable CS8625
            if (other == null)
                return false;
#pragma warning restore CS8625

            return EqualsCore(this, other);
        }

        /// <summary>
        /// Determines whether the current object and another <see cref="IFragmentedFileInfo"/> instance are equal.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IFragmentedFileInfo"/> instance to compare with the current object.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified instance is equal to the current object; otherwise, <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IFragmentedFileInfo other)
        {
            if (other == null)
                return false;

            return EqualsCore(this, other);
        }

        /// <summary>
        /// Determines whether two specified <see cref="IFragmentedFileInfo"/> instances are considered equal.
        /// </summary>
        /// <param name="fileInfo1">
        /// The first <see cref="IFragmentedFileInfo"/> instance to comapre with the second.
        /// </param>
        /// <param name="fileInfo2">
        /// The second <see cref="IFragmentedFileInfo"/> instance to compare with the first.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified instances are equal; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method assumes that <paramref name="fileInfo1"/> and <paramref name="fileInfo2"/> are not <see langword="null"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsCore(IFragmentedFileInfo fileInfo1, IFragmentedFileInfo fileInfo2)
        {
            return fileInfo1.Hash.Equals(fileInfo2.Hash) 
                && fileInfo1.Size == fileInfo2.Size 
                && fileInfo1.FragmentSequence.SequenceEqual(fileInfo2.FragmentSequence);            
        }

        /// <summary>
        /// Determines whether two specified <see cref="FragmentedFileInfo"/> instances are considered equal.
        /// </summary>
        /// <param name="fileInfo1">
        /// The first <see cref="FragmentedFileInfo"/> instance to comapre with the second.
        /// </param>
        /// <param name="fileInfo2">
        /// The second <see cref="FragmentedFileInfo"/> instance to compare with the first.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified instances are equal; otherwise, <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FragmentedFileInfo fileInfo1, FragmentedFileInfo fileInfo2)
        {
            //Paremeters "fileInfo1" and "fileInfo2" can theoretical not be null, because in the project is Nullable enabled, but
            //projects, where nullable is not enabled can still call this function with null values.

#pragma warning disable CS8625
            if (fileInfo1 == null)
                return fileInfo2 == null;
            
            if (fileInfo2 == null)
                return fileInfo1 == null;
#pragma warning restore CS8625

            return EqualsCore(fileInfo1, fileInfo2);
        }

        /// <summary>
        /// Determines whether two specified <see cref="FragmentedFileInfo"/> instances are considered not equal.
        /// </summary>
        /// <param name="fileInfo1">
        /// The first <see cref="FragmentedFileInfo"/> instance to comapre with the second.
        /// </param>
        /// <param name="fileInfo2">
        /// The second <see cref="FragmentedFileInfo"/> instance to compare with the first.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified instances are not equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(FragmentedFileInfo fileInfo1, FragmentedFileInfo fileInfo2)
        {
            //Paremeters "fileInfo1" and "fileInfo2" can theoretical not be null, because in the project is Nullable enabled, but
            //projects, where nullable is not enabled can still call this function with null values.

#pragma warning disable CS8625
            if (fileInfo1 == null)

                return fileInfo2 != null;

            if (fileInfo2 == null)
                return fileInfo1 != null;
#pragma warning restore CS8625

            return !EqualsCore(fileInfo1, fileInfo2);
        }
    }
}

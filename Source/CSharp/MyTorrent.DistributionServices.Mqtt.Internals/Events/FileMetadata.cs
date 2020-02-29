using MyTorrent.DistributionServices.Events.JsonConverters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FileMetadataJsonConverter))]
    public class FileMetadata : IEquatable<FileMetadata>
    {
        private readonly int HashCode;

        public FileMetadata(long size, IEnumerable<string> fragmentSequence)
        {
            Size = size;
            FragmentSequence = fragmentSequence;

            #region CalculateHashCode

            HashCode = 13;

            unchecked
            {
                HashCode = (HashCode * 7) + Size.GetHashCode();

                int i = 0;

                foreach (string fragmentHash in fragmentSequence)
                {
                    HashCode = (HashCode * 7) + Size.GetHashCode() + i++;
                }
            }

            #endregion
        }

        public long Size { get; }

        public IEnumerable<string> FragmentSequence { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is FileMetadata fileMetadata)
                return Equals(fileMetadata);
            
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FileMetadata other)
        {
            if (Size != other.Size)
                return false;

            return FragmentSequence.SequenceEqual(other.FragmentSequence);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FileMetadata? fileMetadata1, FileMetadata? fileMetadata2)
        {
            if (fileMetadata1 is null)
                return fileMetadata2 is null;

            if (fileMetadata2 is null)
                return false;

            return fileMetadata1.Equals(fileMetadata2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FileMetadata? fileMetadata1, FileMetadata? fileMetadata2)
        {
            if (fileMetadata1 is null)
                return !(fileMetadata2 is null);

            if (fileMetadata2 is null)
                return true;

            return !fileMetadata1.Equals(fileMetadata2);
        }
    }
}

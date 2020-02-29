using MyTorrent.DistributionServices.Events.JsonConverters;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FragmentMetadataJsonConverter))]
    public class FragmentMetadata : IEquatable<FragmentMetadata>
    {
        public FragmentMetadata(long size)
        {
            Size = size;
        }

        public long Size { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Size.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is FragmentMetadata fragmentMetadata)
                return Equals(fragmentMetadata);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FragmentMetadata other)
        {
            return Size == other.Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FragmentMetadata? fragmentMetadata1, FragmentMetadata? fragmentMetadata2)
        {
            if (fragmentMetadata1 is null)
                return fragmentMetadata2 is null;

            if (fragmentMetadata2 is null)
                return false;

            return fragmentMetadata1.Equals(fragmentMetadata2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FragmentMetadata? fragmentMetadata1, FragmentMetadata? fragmentMetadata2)
        {
            if (fragmentMetadata1 is null)
                return !(fragmentMetadata2 is null);

            if (fragmentMetadata2 is null)
                return true;

            return !fragmentMetadata1.Equals(fragmentMetadata2);
        }
    }
}

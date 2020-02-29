using MyTorrent.DistributionServices.Events.JsonConverters;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(ClientMetadataJsonConverter))]
    public class ClientMetadata : IEquatable<ClientMetadata>
    {
        private readonly int HashCode;

        public ClientMetadata(IEnumerable<Uri> endpoints, IEnumerable<string> storedFragments)
        {
            Endpoints = new HashSet<Uri>(endpoints);
            StoredFragments = new HashSet<string>(storedFragments);

            #region CalculateHashCode

            HashCode = 13;

            unchecked
            {
                HashCode = (HashCode * 7) + Endpoints.GetHashCode();
                HashCode = (HashCode * 7) + StoredFragments.GetHashCode();
            }

            #endregion
        }

        public ClientMetadata(ISet<Uri> endpoints, ISet<string> storedFragments)
        {
            Endpoints = endpoints;
            StoredFragments = storedFragments;

            #region CalculateHashCode

            HashCode = 13;

            unchecked
            {
                HashCode = (HashCode * 7) + Endpoints.GetHashCode();
                HashCode = (HashCode * 7) + StoredFragments.GetHashCode();
            }

            #endregion
        }

        public ISet<Uri> Endpoints { get; }

        public ISet<string> StoredFragments { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is ClientMetadata clientMetadata)
                return Equals(clientMetadata);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ClientMetadata other)
        {
            return Endpoints.SetEquals(other.Endpoints) && StoredFragments.SetEquals(other.StoredFragments);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ClientMetadata? clientMetadata1, ClientMetadata? clientMetadata2)
        {
            if (clientMetadata1 is null)
                return clientMetadata2 is null;

            if (clientMetadata2 is null)
                return false;

            return clientMetadata1.Equals(clientMetadata2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ClientMetadata? clientMetadata1, ClientMetadata? clientMetadata2)
        {
            if (clientMetadata1 is null)
                return !(clientMetadata2 is null);

            if (clientMetadata2 is null)
                return true;

            return !clientMetadata1.Equals(clientMetadata2);
        }
    }
}

using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    [JsonConverter(typeof(DistributionMetadataJsonConverter))]
    public class DistributionMetadata
    {
        public DistributionMetadata(string hashAlgorithm, long fragmentSize)
        {
            HashAlgorithm = hashAlgorithm;
            FragmentSize = fragmentSize;
        }

        public string HashAlgorithm { get; }

        public long FragmentSize { get; }
    }
}

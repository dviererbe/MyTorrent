using System.Collections.Generic;

namespace MyTorrent.DistributionServices
{
    public class FragmentInfo : IFragmentInfo
    {
        internal readonly Dictionary<string, ClientInfo> _fragmentOwner = new Dictionary<string, ClientInfo>();

        public FragmentInfo(string fragmentHash, long size)
        {
            Hash = fragmentHash;
            Size = size;
        }

        public string Hash { get; }

        public long Size { get; }

        public IEnumerable<IClientInfo> FragmentOwner => _fragmentOwner.Values;
    }
}

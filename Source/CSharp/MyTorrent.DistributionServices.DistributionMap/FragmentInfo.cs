using System.Collections.Generic;

namespace MyTorrent.DistributionServices
{
    public class FragmentInfo
    {
        public FragmentInfo(string fragmentHash, long size)
        {
            Hash = fragmentHash;
            Size = size;

            FragmentOwner = new HashSet<Client>();
        }

        public string Hash { get; }

        public long Size { get; }

        internal HashSet<Client> FragmentOwner { get; }
    }
}

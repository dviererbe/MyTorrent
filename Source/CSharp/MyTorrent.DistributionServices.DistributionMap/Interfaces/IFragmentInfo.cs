using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    public interface IFragmentInfo
    {
        public string Hash { get; }

        public long Size { get; }

        public IEnumerable<IClientInfo> FragmentOwner { get; }


    }
}

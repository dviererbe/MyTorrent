using System;
using System.Collections.Generic;

namespace MyTorrent.DistributionServices
{
    public interface IClientInfo
    {
        public string Id { get; }

        public IEnumerable<Uri> Endpoints { get; }

        public IEnumerable<string> Fragments { get; }

        public bool ContainsFragment(string fragmentHash);
    }
}

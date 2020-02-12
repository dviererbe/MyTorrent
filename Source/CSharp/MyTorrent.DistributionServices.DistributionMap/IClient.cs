using System;
using System.Collections.Generic;

namespace MyTorrent.DistributionServices
{
    public interface IClient
    {
        public string Id { get; }
        public IEnumerable<Uri> Endpoints { get; }
        public IEnumerable<string> Fragments { get; }
    }
}

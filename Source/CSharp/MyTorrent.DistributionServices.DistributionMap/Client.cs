using System;
using System.Linq;
using System.Collections.Generic;

namespace MyTorrent.DistributionServices
{
    internal class ClientInfo : IClientInfo
    {
        private readonly HashSet<string> _fragments;

        public ClientInfo(string id, IEnumerable<Uri> endpoints, HashSet<string> storedFragments)
        {
            Id = id;
            Endpoints = endpoints.Distinct().ToArray();
            
            _fragments = storedFragments;
            
        }

        public string Id { get; }

        public IEnumerable<Uri> Endpoints { get; }

        public IEnumerable<string> Fragments => _fragments;
        
        public bool ContainsFragment(string fragmentHash)
        {
            return _fragments.Contains(fragmentHash);
        }

        public bool AddFragment(string fragment)
        {
            return _fragments.Add(fragment);
        }

        public bool RemoveFragment(string fragment)
        {
            return _fragments.Remove(fragment);
        }
    }
}

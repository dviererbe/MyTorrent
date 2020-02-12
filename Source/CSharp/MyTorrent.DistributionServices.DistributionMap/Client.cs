using System;
using System.Collections.Generic;

namespace MyTorrent.DistributionServices
{
    internal class Client : IClient
    {
        private readonly HashSet<string> _fragments;

        public Client(string id, IEnumerable<Uri> endpoints)
        {
            Id = id;
            Endpoints = endpoints;
            _fragments = new HashSet<string>();
        }

        public string Id { get; }

        public IEnumerable<Uri> Endpoints { get; }

        public IEnumerable<string> Fragments => _fragments;

        public bool AddFragment(string fragment)
        {
            return _fragments.Add(fragment);
        }
    }
}

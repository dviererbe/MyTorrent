using System;
using System.Collections.Generic;
using System.Text;
using MyTorrent.DistributionServices.Events;

namespace MyTorrent.DistributionServices
{
    public class ClientJoinFailedException : Exception
    {
        public ClientJoinFailedException(string clientIdentifier, ClientJoinFailedEvent clientJoinFailed)
            : base($"Client ({clientIdentifier}) failed to join the distribution network.")
        {
            ClientIdentifier = clientIdentifier;
            ClientJoinFailed = clientJoinFailed;
        }

        public string ClientIdentifier { get; }

        public ClientJoinFailedEvent ClientJoinFailed { get; }
    }
}

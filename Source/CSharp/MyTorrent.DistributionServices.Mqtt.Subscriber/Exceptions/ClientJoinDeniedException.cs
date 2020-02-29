using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    public class ClientJoinDeniedException : Exception
    {
        public ClientJoinDeniedException(ClientJoinDeniedEvent clientJoinDeniedEvent)
            : base("Tracker server denied to join distribution service.")
        {
            ClientJoinDeniedEvent = clientJoinDeniedEvent;
        }

        public ClientJoinDeniedEvent ClientJoinDeniedEvent { get; }
    }
}

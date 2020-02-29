using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyTorrent.DistributionServices.Events;

namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServicePublisher
    {
        /// <summary>
        /// Accepted client join request and waiting for <see cref="ClientJoinRequestedEvent"/>
        /// </summary>
        private class WaitForClientJoinResponseState : IMqttDistributionServicePublisherState
        {
            public WaitForClientJoinResponseState(
                EventId eventId, string clientIdentifier, ClientMetadata clientMetadata, TimeSpan timeoutTimeSpan)
            {
                EventId = eventId;

                ClientIdentifier = clientIdentifier;
                ClientMetadata = clientMetadata;

                AddFileInfosToDistributionView = new List<FragmentedFileInfo>();
                AddFragmentInfosToDistributionView = new List<FragmentInfo>();
                AddFiles = new Dictionary<string, FileMetadata>();
                AddFragments = new Dictionary<string, FragmentMetadata>();
                
                TimeoutCancellationTokenSource = new CancellationTokenSource();
                TimeoutTask = Task.Delay(timeoutTimeSpan, TimeoutCancellationTokenSource.Token);
            }

            public bool IsValid => true;

            public  EventId EventId { get; }

            public string ClientIdentifier { get; }

            public ClientMetadata ClientMetadata { get; }

            public Task TimeoutTask { get; }

            public CancellationTokenSource TimeoutCancellationTokenSource { get; }

            public List<FragmentedFileInfo> AddFileInfosToDistributionView { get; }

            public List<FragmentInfo> AddFragmentInfosToDistributionView { get; }

            public Dictionary<string, FileMetadata> AddFiles { get; }

            public Dictionary<string, FragmentMetadata> AddFragments { get; }

            public override string ToString() => "WaitForClientJoinResponse";
        }
    }
}
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
        /// Delivered fragment data to torrent servers and waiting for <see cref="FragmentDistributionObtainedEvent"/> or <see cref="FragmentDistributionFailedEvent"/>.
        /// </summary>
        private class WaitForDistributionDeliveryResponseState : IMqttDistributionServicePublisherState
        {
            public WaitForDistributionDeliveryResponseState(
                EventId eventId, 
                string fragmentHash,
                long fragmentSize,
                HashSet<string> requestors, 
                TaskCompletionSource<IEnumerable<Uri>> taskCompletionSource,
                TimeSpan timeoutTimeSpan)
            {
                EventId = eventId;

                TimeoutCancellationTokenSource = new CancellationTokenSource();
                TimeoutTask = Task.Delay(timeoutTimeSpan, TimeoutCancellationTokenSource.Token);

                TaskCompletionSource = taskCompletionSource;

                FragmentHash = fragmentHash;
                FragmentSize = fragmentSize;

                OpenRequestors = requestors;
                ConfirmedRequestors = new HashSet<string>();
            }

            public bool IsValid => true;

            public EventId EventId { get; }

            public Task TimeoutTask { get; }

            public CancellationTokenSource TimeoutCancellationTokenSource { get; }

            public string FragmentHash { get; }

            public long FragmentSize { get; }
            
            public TaskCompletionSource<IEnumerable<Uri>> TaskCompletionSource { get; }

            public HashSet<string> OpenRequestors { get; }

            public HashSet<string> ConfirmedRequestors { get; }

            public override string ToString() => "WaitForDistributionRequest";
        }
    }
}
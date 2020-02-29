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
        /// Started fragment distribution and waiting for <see cref="FragmentDistributionRequestedEvent"/>.
        /// </summary>
        private class WaitForDistributionRequestsState : IMqttDistributionServicePublisherState
        {
            public WaitForDistributionRequestsState(
                EventId eventId,
                string fragmentHash, 
                byte[] fragmentData,
                TaskCompletionSource<IEnumerable<Uri>> taskCompletionSource,
                TimeSpan timeoutTimeSpan)
            {
                EventId = eventId;

                TimeoutCancellationTokenSource = new CancellationTokenSource();
                TimeoutTask = Task.Delay(timeoutTimeSpan, TimeoutCancellationTokenSource.Token);

                FragmentHash = fragmentHash;
                FragmentData = fragmentData;
                TaskCompletionSource = taskCompletionSource;

                Requestors = new HashSet<string>();
            }

            public bool IsValid => true;

            public EventId EventId { get; }

            public Task TimeoutTask { get; }

            public CancellationTokenSource TimeoutCancellationTokenSource { get; }

            public string FragmentHash { get; }

            public long FragmentSize => FragmentData.LongLength;

            public byte[] FragmentData { get; }

            public TaskCompletionSource<IEnumerable<Uri>> TaskCompletionSource { get; }

            public HashSet<string> Requestors { get; }

            public override string ToString() => "WaitForDistributionRequest";
        }
    }
}
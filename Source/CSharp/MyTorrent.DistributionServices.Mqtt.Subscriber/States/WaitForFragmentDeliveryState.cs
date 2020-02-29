using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyTorrent.DistributionServices.Events;

namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client is registered, requested a fragment and waits for an <see cref="FragmentDistributionDeliveredEvent"/>.
        /// </summary>
        private class WaitForFragmentDeliveryState : IMqttDistributionServiceSubscriberState
        {
            public WaitForFragmentDeliveryState(EventId eventId, string fragmentHash, long fragmentSize)
            {
                EventId = eventId;

                TimeoutCancellationTokenSource = new CancellationTokenSource();
                TimeoutTask = Task.Delay(TimeoutTimeSpan, TimeoutCancellationTokenSource.Token);

                FragmentHash = fragmentHash;
                FragmentSize = fragmentSize;
            }

            public bool IsInvalid => false;

            public bool IsRegistered => true;

            public EventId EventId { get; }


            public Task TimeoutTask { get; }

            public CancellationTokenSource TimeoutCancellationTokenSource { get; }

            public string FragmentHash { get; }

            public long FragmentSize { get; }

            public override string ToString() => "WaitForFragmentDelivery";
        }
    }
}
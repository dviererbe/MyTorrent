using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyTorrent.DistributionServices.Events;

namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client is not registered yet and tries joining the distribution service.
        /// Waiting for <see cref="ClientJoinAcceptedEvent"/> or <see cref="ClientJoinDeniedEvent"/>.
        /// </summary>
        private class WaitForJoinResponseState : IMqttDistributionServiceSubscriberState
        {
            public WaitForJoinResponseState(EventId eventId)
            {
                EventId = eventId;

                TimeoutCancellationTokenSource = new CancellationTokenSource();
                TimeoutTask = Task.Delay(TimeoutTimeSpan, TimeoutCancellationTokenSource.Token);
            }

            public bool IsInvalid => false;

            public bool IsRegistered => false;

            public EventId EventId { get; }

            public Task TimeoutTask { get; }

            public CancellationTokenSource TimeoutCancellationTokenSource { get; }

            public override string ToString() => "WaitForJoinResponse";
        }
    }
}
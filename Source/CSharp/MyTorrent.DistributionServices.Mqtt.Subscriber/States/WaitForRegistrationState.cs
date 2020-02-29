using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyTorrent.DistributionServices.Events;

namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client is not registered yet and waits for <see cref="ClientRegisteredEvent"/>.
        /// </summary>
        private class WaitForRegistrationState : IMqttDistributionServiceSubscriberState
        {
            public WaitForRegistrationState(
                EventId eventId, Task timeoutTask, CancellationTokenSource timeoutCancellationTokenSource, IEnumerable<string> removeFragments)
            {
                EventId = eventId;

                TimeoutCancellationTokenSource = timeoutCancellationTokenSource;
                TimeoutTask = TimeoutTask;

                RemoveFragments = removeFragments;
            }

            public bool IsInvalid => false;

            public bool IsRegistered => false;

            public EventId EventId { get; }

            public Task TimeoutTask { get; }

            public CancellationTokenSource TimeoutCancellationTokenSource { get; }

            public IEnumerable<string> RemoveFragments { get; }

            public override string ToString() => "WaitForRegistration";
        }
    }
}
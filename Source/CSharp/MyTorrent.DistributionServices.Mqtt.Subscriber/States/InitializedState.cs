using MyTorrent.DistributionServices.Events;

namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client is not registered and waits for an <see cref="TrackerHelloEvent"/> to try joining the distribution service.
        /// </summary>
        private struct InitializedState : IMqttDistributionServiceSubscriberState
        {
            public bool IsInvalid => false;

            public bool IsRegistered => false;

            public override string ToString() => "Initialized";
        }
    }
}
namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client is registered and processes currently no event.
        /// </summary>
        private struct IdleState : IMqttDistributionServiceSubscriberState
        {
            public bool IsInvalid => false;

            public bool IsRegistered => true;

            public override string ToString() => "Idle";
        }
    }
}
namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client is currently initializing.
        /// </summary>
        private struct InitializingState : IMqttDistributionServiceSubscriberState
        {
            public bool IsInvalid => true;

            public bool IsRegistered => false;

            public override string ToString() => "Initializing";
        }
    }
}
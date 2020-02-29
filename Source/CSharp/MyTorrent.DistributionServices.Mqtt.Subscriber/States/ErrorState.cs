namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client is in an unrecoverable error state.
        /// </summary>
        private struct ErrorState : IMqttDistributionServiceSubscriberState
        {
            public bool IsInvalid => true;

            public bool IsRegistered => false;

            public override string ToString() => "Error";
        }
    }
}
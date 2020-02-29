namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        /// <summary>
        /// Client was disposed. State is unrecoverable.
        /// </summary>
        private struct DisposedState : IMqttDistributionServiceSubscriberState
        {
            public bool IsInvalid => true;

            public bool IsRegistered => false;

            public override string ToString() => "Disposed";
        }
    }
}
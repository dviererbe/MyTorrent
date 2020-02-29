namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServiceSubscriber
    {
        private interface IMqttDistributionServiceSubscriberState
        {
            public bool IsInvalid { get; }
            public bool IsRegistered { get; }
        }
    }
}
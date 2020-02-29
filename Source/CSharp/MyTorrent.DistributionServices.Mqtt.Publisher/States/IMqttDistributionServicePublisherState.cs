namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServicePublisher
    {
        private interface IMqttDistributionServicePublisherState
        {
            public bool IsValid { get; }
        }
    }
}
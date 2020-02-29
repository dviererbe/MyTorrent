namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServicePublisher
    {
        /// <summary>
        /// <see cref="MqttDistributionServicePublisher"/> is currently initializing.
        /// </summary>
        private struct InitializingState : IMqttDistributionServicePublisherState
        {
            public bool IsValid => false;

            public override string ToString() => "Initializing";
        }
    }
}
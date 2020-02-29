namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServicePublisher
    {
        /// <summary>
        /// Unrecoverable error state.
        /// </summary>
        private struct ErrorState : IMqttDistributionServicePublisherState
        {
            public bool IsValid => false;

            public override string ToString() => "Error";
        }
    }
}
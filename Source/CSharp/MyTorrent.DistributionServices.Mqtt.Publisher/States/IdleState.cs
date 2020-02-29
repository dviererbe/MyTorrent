namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServicePublisher
    {
        /// <summary>
        /// Idle state; no event is currently processed.
        /// </summary>
        private struct IdleState : IMqttDistributionServicePublisherState
        {
            public bool IsValid => true;

            public override string ToString() => "Idle";
        }
    }
}
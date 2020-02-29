namespace MyTorrent.DistributionServices
{
    public partial class MqttDistributionServicePublisher
    {
        /// <summary>
        /// <see cref="MqttDistributionServicePublisher"/> was disposed. Unrecoverable state.
        /// </summary>
        private struct DisposedState : IMqttDistributionServicePublisherState
        {
            public bool IsValid => false;

            public override string ToString() => "Disposed";
        }
    }
}
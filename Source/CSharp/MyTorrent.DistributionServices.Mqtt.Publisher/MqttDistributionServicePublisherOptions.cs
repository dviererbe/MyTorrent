namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Configuration options for a <see cref="MqttDistributionServicePublisher"/> instance.
    /// </summary>
    public class MqttDistributionServicePublisherOptions
    {
        /// <summary>
        /// Default options for the <see cref="MqttDistributionServicePublisher"/>.
        /// </summary>
        public static readonly MqttDistributionServicePublisherOptions Default = new MqttDistributionServicePublisherOptions();

        /// <summary>
        /// Port for the Mqtt Server where Mqtt clients can connect to.
        /// </summary>
        public int Port { get; set; } = 1809;

        /// <summary>
        /// The maximum size a fragment is allowed to have.
        /// </summary>
        /// <remarks>
        /// All fragments, expect the last fragment of an fragmented file have to be exactly of this size.
        /// But the last fragment must not be empty.
        /// </remarks>
        public long FragmentSize { get; set; } = 80_000;
    }
}

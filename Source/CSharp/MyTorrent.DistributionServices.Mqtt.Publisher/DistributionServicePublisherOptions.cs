namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Configuration options for a <see cref="MqttDistributionServicePublisher"/> instance.
    /// </summary>
    public class DistributionServicePublisherOptions
    {
        /// <summary>
        /// Default options for the <see cref="DistributionServicePublisherOptions"/>.
        /// </summary>
        public static readonly DistributionServicePublisherOptions Default = new DistributionServicePublisherOptions();

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

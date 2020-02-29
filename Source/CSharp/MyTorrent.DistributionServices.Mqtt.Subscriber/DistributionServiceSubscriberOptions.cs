using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Configuration options for a <see cref="MqttDistributionServiceSubscriber"/> instance.
    /// </summary>
    public class DistributionServiceSubscriberOptions
    {
        /// <summary>
        /// Default options for the <see cref="DistributionServiceSubscriberOptions"/>.
        /// </summary>
        public static readonly DistributionServiceSubscriberOptions Default = new DistributionServiceSubscriberOptions();

        /// <summary>
        /// Gets the timespan in millisecond how long the publisher will wait for responding events of the subscribers.
        /// </summary>
        public int Timeout { get; set; } = 5000;
    }
}

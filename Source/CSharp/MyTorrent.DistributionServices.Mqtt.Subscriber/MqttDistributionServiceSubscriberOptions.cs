using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Configuration options for a <see cref="MqttDistributionServiceSubscriber"/> instance.
    /// </summary>
    public class MqttDistributionServiceSubscriberOptions
    {
        /// <summary>
        /// Default options for the <see cref="MqttDistributionServiceSubscriber"/>.
        /// </summary>
        public static readonly MqttDistributionServiceSubscriberOptions Default = new MqttDistributionServiceSubscriberOptions();

        /// <summary>
        /// Port of the Mqtt Server the Mqtt clients should connect to.
        /// </summary>
        public int Port { get; set; } = 1809;
    }
}

using MQTTnet;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An IMqtt Endpoint to publish and receive messages.
    /// </summary>
    public interface IMqttEndpoint : IApplicationMessagePublisher, IApplicationMessageReceiver, IDisposable
    {
        /// <summary>
        /// Gets the identifier used by the Mqtt Endpoint.
        /// </summary>
        public string ClientId { get; }
    }
}

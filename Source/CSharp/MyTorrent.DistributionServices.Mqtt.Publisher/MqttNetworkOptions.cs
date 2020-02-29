namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// The type of Mqtt Broker the <see cref="MqttDistributionServicePublisher"/> should use.
    /// </summary>
    public enum BrokerType
    {
        /// <summary>
        /// The Mqtt Broker is hosted by the <see cref="MqttDistributionServicePublisher"/> itself.
        /// </summary>
        SelfHosted = 1,

        /// <summary>
        /// The distribution <see cref="MqttDistributionServicePublisher"/> uses a mqtt client to connect to a remote Mqtt Broker.
        /// </summary>
        Remote = 2
    }

    /// <summary>
    /// Configuration options for Mqtt network.
    /// </summary>
    public class MqttNetworkOptions
    {
        /// <summary>
        /// Default options for the <see cref="MqttNetworkOptions"/>.
        /// </summary>
        public static readonly MqttNetworkOptions Default = new MqttNetworkOptions();

        /// <summary>
        /// Gets the type of the Mqtt Broker that should be used for the <see cref="MqttDistributionServicePublisher"/>.
        /// </summary>
        public BrokerType BrokerType { get; set; } = BrokerType.SelfHosted;

        /// <summary>
        /// Gets the port of the self hosted mqtt broker where the clients can connect to if <see cref="BrokerType"/> is <see cref="BrokerType.SelfHosted"/>.
        /// -or- Gets the port of the remote mqtt broker the mqtt client should connect to if <see cref="BrokerType"/> is <see cref="BrokerType.Remote"/>.
        /// </summary>
        public int Port { get; set; } = 1809;

        /// <summary>
        /// Gets the host address of the remote mqtt broker the mqtt client should connect to if <see cref="BrokerType"/> is <see cref="BrokerType.Remote"/>;
        /// <see langword="null"/> if <see cref="BrokerType"/> is <see cref="BrokerType.SelfHosted"/>.
        /// </summary>
        /// <remarks>
        /// This property will be ignored if <see cref="BrokerType"/> is <see cref="BrokerType.SelfHosted"/>.
        /// </remarks>
        public string? Host { get; set; } = "localhost";

        /// <summary>
        /// Gets the timespan in millisecond how long the mqtt endpoint will wait for a QoS responses like PUBACK.
        /// </summary>
        public int Timeout { get; set; } = 2000;
    }
}

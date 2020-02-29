namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// The type of Mqtt Broker the <see cref="MqttDistributionServicePublisher"/> should use.
    /// </summary>
    public enum MqttBrokerType
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
        public MqttBrokerType MqttBrokerType { get; set; } = MqttBrokerType.SelfHosted;

        /// <summary>
        /// Gets the port of the self hosted mqtt broker where the clients can connect to if <see cref="MqttBrokerType"/> is <see cref="MqttBrokerType.SelfHosted"/>.
        /// -or- Gets the port of the remote mqtt broker the mqtt client should connect to if <see cref="MqttBrokerType"/> is <see cref="MqttBrokerType.Remote"/>.
        /// </summary>
        public int Port { get; set; } = 1809;

        /// <summary>
        /// Gets the host address of the remote mqtt broker the mqtt client should connect to if <see cref="MqttBrokerType"/> is <see cref="MqttBrokerType.Remote"/>;
        /// <see langword="null"/> if <see cref="MqttBrokerType"/> is <see cref="MqttBrokerType.SelfHosted"/>.
        /// </summary>
        /// <remarks>
        /// This property will be ignored if <see cref="MqttBrokerType"/> is <see cref="MqttBrokerType.SelfHosted"/>.
        /// </remarks>
        public string? Host { get; set; } = "localhost";
    }
}

namespace MyTorrent.DistributionServices
{
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
        /// Gets the port of the remote mqtt broker the mqtt client should connect to.
        /// </summary>
        public int Port { get; set; } = 1809;

        /// <summary>
        /// Gets the host address of the remote mqtt broker the mqtt client should connect to.
        /// </summary>
        public string? Host { get; set; } = "localhost";
    }
}

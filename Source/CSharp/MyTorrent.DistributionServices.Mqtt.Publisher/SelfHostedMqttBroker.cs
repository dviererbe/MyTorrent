using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using MQTTnet;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;

namespace MyTorrent.DistributionServices
{
    public class SelfHostedMqttBroker : IMqttEndpoint
    {
        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<SelfHostedMqttBroker> _logger;

        private readonly IMqttServer _mqttServer;

        private volatile bool _disposed = false;

        public SelfHostedMqttBroker(
            ILogger<SelfHostedMqttBroker> logger,
            IEventIdCreationSource eventIdCreationSource,
            int port,
            int timeout)
        {
            _logger = logger;
            _eventIdCreationSource = eventIdCreationSource;

            EventId eventId = GetNextEventId();
#if DEBUG
            _logger.LogDebug(eventId, "Creating Mqtt Server.");
#endif
            if (port < 0x0000)
                throw new ArgumentOutOfRangeException(nameof(port), port, "Port number too small.");
            else if (timeout > 0xffff)
                throw new ArgumentOutOfRangeException(nameof(port), port, "Port number too large.");

            if (timeout < 1)
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout timespan too small.");
            else if (timeout > 99999)
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout timespan too large.");

            _logger.LogInformation(eventId, $"MQTT Server Communication Timeout: {timeout} ms.");

            var serverOptionsBuilder = new MqttServerOptionsBuilder()
                .WithClientId(ClientId)
                .WithoutEncryptedEndpoint()
                .WithConnectionBacklog(100)
                .WithDefaultCommunicationTimeout(TimeSpan.FromMilliseconds(timeout))
                .WithDefaultEndpointPort(port);

            _mqttServer = new MqttFactory().CreateMqttServer();
            _mqttServer.UseClientConnectedHandler(HandleClientConnectedAsync);
            _mqttServer.UseClientDisconnectedHandler(HandleClientDisconnectedAsync);

            try
            {
                _logger.LogInformation(eventId, $"Starting Mqtt-Server (mqtt://0.0.0.0:{port}).");
                _mqttServer.StartAsync(serverOptionsBuilder.Build()).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                string errorMessage = "Failed to start Mqtt-Server.";
                _logger.LogCritical(eventId, exception, errorMessage);
                throw new Exception(errorMessage); ;
            }
        }

        ~SelfHostedMqttBroker()
        {
            Dispose(false);
        }

        public string ClientId { get; } = Guid.NewGuid().ToString("N");

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureNotDisposed();
                return _mqttServer.ApplicationMessageReceivedHandler;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                EnsureNotDisposed();
                _mqttServer.ApplicationMessageReceivedHandler = value;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Gets the next unique event id.
        /// </summary>
        /// <param name="name">
        /// The name of the event (optional).
        /// </param>
        /// <returns>
        /// The unique event id.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventId GetNextEventId(string? name = null) => _eventIdCreationSource.GetNextId(name);

        /// <summary>
        /// Ensures that this <see cref="SelfHostedMqttBroker"/> instance was not disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this <see cref="SelfHostedMqttBroker"/> instance was disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    objectName: GetType().FullName,
                    message: "Remote Mqtt-Broker was already disposed.");
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            try
            {
                EnsureNotDisposed();
                return _mqttServer.PublishAsync(applicationMessage, cancellationToken);
            }
            catch (Exception exception)
            {
                return Task.FromException<MqttClientPublishResult>(exception);
            }
        }

        public void Dispose()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        private Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
        {
            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, $"Client connected (Id: {eventArgs.ClientId})");

            return Task.CompletedTask;
        }

        private Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, $"Client disconnected (Id: {eventArgs.ClientId})");

            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            EventId eventId = GetNextEventId();

            try
            {
                _logger.LogInformation(eventId, "Stopping Mqtt-Server.");
                _mqttServer.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                _logger.LogError(eventId, exception, "Stopping Mqtt-Server failed.");
            }
        }
    }
}

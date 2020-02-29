using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;

namespace MyTorrent.DistributionServices
{
    public class RemoteMqttBroker : IMqttEndpoint
    {
        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<RemoteMqttBroker> _logger;

        private readonly IMqttClient _mqttClient;

        private volatile bool _disposed = false;

        public RemoteMqttBroker(
            ILogger<RemoteMqttBroker> logger, 
            IEventIdCreationSource eventIdCreationSource,
            string host,
            int port)
        {
            _logger = logger;
            _eventIdCreationSource = eventIdCreationSource;

            EventId eventId = GetNextEventId();
#if DEBUG
            _logger.LogDebug(eventId, "Creating Mqtt Client.");
#endif

            var clientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(ClientId)
                .WithTcpServer(host, port);

            _mqttClient = new MqttFactory().CreateMqttClient();
            
            CancellationTokenSource? cancellationTokenSource = null;

            #region Connecting to remote Mqtt Broker

            try
            {
                _logger.LogInformation(eventId, $"Connecting to remote Mqtt-Broker (mqtt://{host}:{port}).");
                
                cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)); //timeout
                var result = _mqttClient.ConnectAsync(clientOptionsBuilder.Build(), cancellationTokenSource.Token).GetAwaiter().GetResult();

                if (result.ResultCode != MqttClientConnectResultCode.Success)
                {
                    throw new Exception("Mqtt-Client ConnectResultCode Success expected. Actual: " + result.ResultCode);
                }
            }
            catch (OperationCanceledException)
            {
                string errorMessage = "Connect to remote Mqtt-Broker timed out.";
                _logger.LogCritical(eventId, errorMessage);
                throw new Exception(errorMessage);
            }
            catch (Exception exception)
            {
                string errorMessage = "Failed to connect to remote Mqtt-Broker.";
                _logger.LogCritical(eventId, exception, errorMessage);
                throw new Exception(errorMessage);
            }
            finally
            {
                cancellationTokenSource?.Dispose();
            }

            #endregion

            #region Subscribing to relevant Events

            try
            {
                _logger.LogInformation(eventId, $"Subscribing to relevant events.");

                MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
                MqttClientSubscribeResultCode expectedSubscribeResultCode = MqttClientSubscribeResultCode.GrantedQoS1;

                MqttClientSubscribeOptionsBuilder subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(MqttTopics.ClientJoinRequested, mqttQualityOfServiceLevel)
                    .WithTopicFilter(MqttTopics.ClientJoinSucceeded, mqttQualityOfServiceLevel)
                    .WithTopicFilter(MqttTopics.ClientJoinFailed, mqttQualityOfServiceLevel)
                    .WithTopicFilter(MqttTopics.ClientGoodbye, mqttQualityOfServiceLevel)
                    .WithTopicFilter(MqttTopics.FragmentDistributionRequested, mqttQualityOfServiceLevel)
                    .WithTopicFilter(MqttTopics.FragmentDistributionObtained, mqttQualityOfServiceLevel)
                    .WithTopicFilter(MqttTopics.FragmentDistributionFailed, mqttQualityOfServiceLevel);

                cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)); //timeout
                MqttClientSubscribeResult result = _mqttClient.SubscribeAsync(subscribeOptionsBuilder.Build(), cancellationTokenSource.Token).GetAwaiter().GetResult();

                foreach (MqttClientSubscribeResultItem item in result.Items)
                {
                    if (item.ResultCode != expectedSubscribeResultCode)
                    {
                        throw new Exception($"Expected SubscribeResultCode {expectedSubscribeResultCode} for topic \"{item.TopicFilter.Topic}\". Actual: {item.ResultCode}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                string errorMessage = "Subscribing to all relevant Events to remote Mqtt-Broker timed out.";
                _logger.LogCritical(eventId, errorMessage);

                DisconnectOnError(errorMessage);

                throw new Exception(errorMessage);
            }
            catch (Exception exception)
            {
                string errorMessage = "Failed to subscribe to all relevant topics with expected SubscribeResultCode.";
                _logger.LogCritical(eventId, exception, errorMessage);

                DisconnectOnError(errorMessage);

                throw new Exception(errorMessage);
            }
            finally
            {
                cancellationTokenSource?.Dispose();
            }

            #endregion

            void DisconnectOnError(string reasonString)
            {
                try
                {
                    _logger.LogInformation("Disconnecting from remote Mqtt-Broker");
                    MqttClientDisconnectOptions disconnectOptions = new MqttClientDisconnectOptions()
                    {
                        ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                        ReasonString = reasonString
                    };

                    cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    _mqttClient.DisconnectAsync(disconnectOptions, cancellationTokenSource.Token).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError(eventId, "Disconnecting from remote Mqtt-Broker timed out.");
                }
                catch (Exception exception)
                {
                    _logger.LogError(eventId, exception, "Disconnecting from remote Mqtt-Broker failed.");
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                }
            }
        }

        ~RemoteMqttBroker()
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
                return _mqttClient.ApplicationMessageReceivedHandler;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                EnsureNotDisposed();
                _mqttClient.ApplicationMessageReceivedHandler = value;
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
        /// Ensures that this <see cref="RemoteMqttBroker"/> instance was not disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this <see cref="RemoteMqttBroker"/> instance was disposed.
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
                return _mqttClient.PublishAsync(applicationMessage, cancellationToken);
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

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            EventId eventId = GetNextEventId();

            CancellationTokenSource? cancellationTokenSource = null;

            try
            {
                try
                {
                    MqttClientUnsubscribeOptions mqttClientUnsubscribeOptions = new MqttClientUnsubscribeOptions
                    {
                        TopicFilters = new List<string>
                        {
                            MqttTopics.ClientJoinRequested,
                            MqttTopics.ClientJoinSucceeded,
                            MqttTopics.ClientJoinFailed,
                            MqttTopics.ClientGoodbye,
                            MqttTopics.FragmentDistributionRequested,
                            MqttTopics.FragmentDistributionObtained,
                            MqttTopics.FragmentDistributionFailed
                        }
                    };

                    _logger.LogInformation(eventId, "Unsubscribing from all events from remote Mqtt-Broker.");
                    cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var result = _mqttClient.UnsubscribeAsync(mqttClientUnsubscribeOptions, cancellationTokenSource.Token).GetAwaiter().GetResult();

                    foreach (MqttClientUnsubscribeResultItem item in result.Items)
                    {
                        if (item.ReasonCode != MqttClientUnsubscribeResultCode.Success)
                        {
                            throw new Exception($"Expected UnsubscribeResultCode Success for topic \"{item.TopicFilter}\". Actual: {item.ReasonCode}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError(eventId, "Unsubscribing from all events from remote Mqtt-Broker timed out.");
                }
                catch (Exception exception)
                {
                    _logger.LogError(eventId, exception, "Unsubscribing from all events from remote Mqtt-Broker failed.");
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                }

                _logger.LogInformation(eventId, "Disconnecting from remote Mqtt-Broker.");
                MqttClientDisconnectOptions disconnectOptions = new MqttClientDisconnectOptions()
                {
                    ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                    ReasonString = "Client Disposed"
                };

                cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                _mqttClient.DisconnectAsync(disconnectOptions, cancellationTokenSource.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                _logger.LogError(eventId, "Disconnecting from remote Mqtt-Broker timed out.");
            }
            catch (Exception exception)
            {
                _logger.LogError(eventId, exception, "Disconnecting from remote Mqtt-Broker failed.");
            }
            finally
            {
                _mqttClient.Dispose();
                cancellationTokenSource?.Dispose();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Client;
using MQTTnet.Client.Publishing;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MyTorrent.DistributionServices.Events
{
    public static class EventExtensions
    {
        public static MqttClientPublishResult PublishEvent(
            this IApplicationMessagePublisher applicationMessagePublisher,
            EventBase @event,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            return applicationMessagePublisher.PublishAsync(@event.BuildMqttMessage(), cancellationToken).GetAwaiter().GetResult();
        }

        public static Task<MqttClientPublishResult> PublishEventAsync(
            this IApplicationMessagePublisher applicationMessagePublisher, 
            EventBase @event, 
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<MqttClientPublishResult>(cancellationToken);

            return applicationMessagePublisher.PublishAsync(@event.BuildMqttMessage(), cancellationToken);
        }
    }
}

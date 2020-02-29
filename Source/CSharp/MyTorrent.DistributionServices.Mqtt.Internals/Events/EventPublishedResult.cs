using MQTTnet.Client.Publishing;

namespace MyTorrent.DistributionServices.Events
{
    public class EventPublishedResult<EventType> where EventType : EventBase
    {
        public EventPublishedResult(EventType publishedEvent, MqttClientPublishResult publishResult)
        {
            PublishedEvent = publishedEvent;
            PublishResult = publishResult;
        }

        public EventType PublishedEvent { get; }

        public MqttClientPublishResult PublishResult { get; }
    }
}

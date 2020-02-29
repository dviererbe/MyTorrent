using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    public abstract class EventBase
    {
        public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = false,
            IgnoreNullValues = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        protected EventBase(Guid? eventId)
        {
            EventId = eventId ?? Guid.NewGuid();
        }

        /// <summary>
        /// Gets the unique identifier of the event.
        /// </summary>
        /// <remarks>
        /// Can be used to avoid processing of duplicates or in logging for monitoring and/or debugging.
        /// </remarks>
        public Guid EventId { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => ToJsonString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string ToJsonString(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Serialize(this, GetType(), jsonSerializerOptions);
        }

        protected MqttApplicationMessage BuildMqttMessageCore(string topic, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;

            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(this, GetType(), jsonSerializerOptions);

            MqttApplicationMessage mqttMessage = new MqttApplicationMessage()
            {
                Topic = topic,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
                Payload = payload
            };

            return mqttMessage;
        }

        public abstract MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventType FromUtf8Bytes<EventType>(
            ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null) where EventType : EventBase
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<EventType>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEventType FromJsonString<TEventType>(
            string json, JsonSerializerOptions? jsonSerializerOptions = null) where TEventType : EventBase
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<TEventType>(json, jsonSerializerOptions);
        }
    }
}

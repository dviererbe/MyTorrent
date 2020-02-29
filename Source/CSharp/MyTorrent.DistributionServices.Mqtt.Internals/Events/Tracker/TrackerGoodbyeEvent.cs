using MQTTnet;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System;
using MyTorrent.DistributionServices.Events.JsonConverters;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(TrackerGoodbyeEventJsonConverter))]
    public class TrackerGoodbyeEvent : EventBase
    {
        public TrackerGoodbyeEvent(Guid? eventId = null) : base(eventId)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.TrackerGoodbye, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TrackerGoodbyeEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<TrackerGoodbyeEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TrackerGoodbyeEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<TrackerGoodbyeEvent>(json, jsonSerializerOptions);
        }
    }
}

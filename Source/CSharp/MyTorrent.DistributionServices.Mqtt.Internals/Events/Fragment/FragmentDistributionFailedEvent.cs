using MQTTnet;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System;
using System.Text.Json.Serialization;
using MyTorrent.DistributionServices.Events.JsonConverters;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FragmentDistributionFailedEventJsonConverter))]
    public class FragmentDistributionFailedEvent : EventBase
    {
        public FragmentDistributionFailedEvent(string hash, Guid? eventId = null) : base(eventId)
        {
            Hash = hash;
        }

        public string Hash { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.FragmentDistributionFailed, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionFailedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionFailedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionFailedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionFailedEvent>(json, jsonSerializerOptions);
        }
    }
}

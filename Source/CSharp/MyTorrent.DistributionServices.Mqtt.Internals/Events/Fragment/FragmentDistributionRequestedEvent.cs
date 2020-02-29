using MQTTnet;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System;
using MyTorrent.DistributionServices.Events.JsonConverters;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FragmentDistributionRequestedEventJsonConverter))]
    public class FragmentDistributionRequestedEvent : EventBase
    {
        public FragmentDistributionRequestedEvent(
            string hash,
            Guid? eventId = null)
            : base(eventId)
        {
            Hash = hash;
        }

        public string Hash { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.FragmentDistributionRequested, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionRequestedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionRequestedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionRequestedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionRequestedEvent>(json, jsonSerializerOptions);
        }
    }
}

using MQTTnet;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System;
using MyTorrent.DistributionServices.Events.JsonConverters;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FragmentDistributionObtainedEventJsonConverter))]
    public class FragmentDistributionObtainedEvent : EventBase
    {
        public FragmentDistributionObtainedEvent(string hash, Guid? eventId = null) : base(eventId)
        {
            Hash = hash;
        }

        public string Hash { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.FragmentDistributionObtained, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionObtainedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionObtainedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionObtainedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionObtainedEvent>(json, jsonSerializerOptions);
        }
    }
}

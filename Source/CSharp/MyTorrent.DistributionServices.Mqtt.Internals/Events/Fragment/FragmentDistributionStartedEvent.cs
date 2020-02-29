using MQTTnet;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using MyTorrent.DistributionServices.Events.JsonConverters;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FragmentDistributionStartedEventJsonConverter))]
    public class FragmentDistributionStartedEvent : EventBase
    {
        public FragmentDistributionStartedEvent(
            string hash,
            long size,
            Guid? eventId = null)
            : base(eventId)
        {
            Hash = hash;
            Size = size;
        }

        public string Hash { get; }

        public long Size { get; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.FragmentDistributionStarted, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionStartedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionStartedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionStartedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionStartedEvent>(json, jsonSerializerOptions);
        }
    }
}

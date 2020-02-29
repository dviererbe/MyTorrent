using MQTTnet;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.Text.Json.Serialization;
using MyTorrent.DistributionServices.Events.JsonConverters;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FragmentDistributionEndedEventJsonConverter))]
    public class FragmentDistributionEndedEvent : EventBase
    {
        public FragmentDistributionEndedEvent(
            string hash,
            long size,
            ISet<string> receivers,
            Guid? eventId = null)
            : base(eventId)
        {
            Hash = hash;
            Size = size;
            Receivers = receivers;
        }

        public string Hash { get; }

        public long Size { get; }

        public ISet<string> Receivers { get; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.FragmentDistributionEnded, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionEndedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionEndedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionEndedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionEndedEvent>(json, jsonSerializerOptions);
        }
    }
}

using MQTTnet;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MyTorrent.DistributionServices.Events.JsonConverters;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FragmentDistributionDeliveredEventJsonConverter))]
    public class FragmentDistributionDeliveredEvent : EventBase
    {
        public FragmentDistributionDeliveredEvent(
            string hash,
            byte[] data,
            ISet<string> receivers,
            Guid? eventId = null) 
            : base(eventId)
        {
            Hash = hash;
            Data = data;
            Receivers = receivers;
        }

        public string Hash { get; }
        
        public byte[] Data { get; }

        public ISet<string> Receivers { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.FragmentDistributionDelivered, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionDeliveredEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionDeliveredEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FragmentDistributionDeliveredEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FragmentDistributionDeliveredEvent>(json, jsonSerializerOptions);
        }
    }
}

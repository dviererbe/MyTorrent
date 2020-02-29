using MQTTnet;
using MyTorrent.DistributionServices.Events.JsonConverters;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(ClientJoinSucceededEventJsonConverter))]
    public class ClientJoinSucceededEvent : EventBase
    {
        public ClientJoinSucceededEvent(Guid? eventId = null) : base(eventId)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.ClientJoinSucceeded, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinSucceededEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinSucceededEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinSucceededEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinSucceededEvent>(json, jsonSerializerOptions);
        }
    }
}

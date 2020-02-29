using MQTTnet;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System;
using MyTorrent.DistributionServices.Events.JsonConverters;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(ClientGoodbyeEventJsonConverter))]
    public class ClientGoodbyeEvent : EventBase
    {
        public ClientGoodbyeEvent(Guid? eventId = null) : base(eventId)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.ClientGoodbye, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientGoodbyeEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientGoodbyeEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientGoodbyeEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientGoodbyeEvent>(json, jsonSerializerOptions);
        }
    }
}

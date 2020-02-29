using MQTTnet;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System;
using System.Text.Json.Serialization;
using MyTorrent.DistributionServices.Events.JsonConverters;

namespace MyTorrent.DistributionServices.Events
{

    [JsonConverter(typeof(ClientJoinDeniedEventJsonConverter))]
    public class ClientJoinDeniedEvent : EventBase
    {
        public ClientJoinDeniedEvent(
            string clientIdentifier,
            ClientJoinDeniedCode reasonCode,
            string? reason = null,
            Guid? eventId = null) 
            : base(eventId)
        {
            ClientIdentifier = clientIdentifier;
            ReasonCode = reasonCode;
            Reason = reason;

            if (!Enum.IsDefined(typeof(ClientJoinDeniedCode), reasonCode))
            {
                throw new ArgumentOutOfRangeException(nameof(reasonCode), reasonCode, "Undefined enum value!");
            }
        }

        public string ClientIdentifier { get; }

        public ClientJoinDeniedCode ReasonCode { get; }

        public string? Reason { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.ClientJoinDenied, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinDeniedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinDeniedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinDeniedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinDeniedEvent>(json, jsonSerializerOptions);
        }
    }
}

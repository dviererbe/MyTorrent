using MQTTnet;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using MyTorrent.DistributionServices.Events.JsonConverters;
using System.Text.Json.Serialization;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(ClientRegisteredEventJsonConverter))]
    public class ClientRegisteredEvent : EventBase
    {
        public ClientRegisteredEvent(
            string clientIdentifier,
            ClientMetadata info,
            IDictionary<string, FileMetadata> addedFiles,
            IDictionary<string, FragmentMetadata> addedFragments,
            Guid? eventId = null) 
            : base(eventId)
        {
            ClientIdentifier = clientIdentifier;
            Info = info;
            AddedFiles = addedFiles;
            AddedFragments = addedFragments;
        }

        public string ClientIdentifier { get; }

        public ClientMetadata Info { get; }

        public IDictionary<string, FileMetadata> AddedFiles { get; }

        public IDictionary<string, FragmentMetadata> AddedFragments { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.ClientRegistered, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientRegisteredEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientRegisteredEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientRegisteredEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientRegisteredEvent>(json, jsonSerializerOptions);
        }
    }
}

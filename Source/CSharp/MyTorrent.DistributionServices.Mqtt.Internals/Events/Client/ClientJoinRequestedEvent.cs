using MQTTnet;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using MyTorrent.DistributionServices.Events.JsonConverters;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(ClientJoinRequestedEventJsonConverter))]
    public class ClientJoinRequestedEvent : EventBase
    {
        public ClientJoinRequestedEvent(
            IDictionary<string, FileMetadata> knownFileInfos,
            IDictionary<string, FragmentMetadata> storedFragments,
            ISet<Uri> endpoints,
            string hashAlgorithm,
            long? fragmentSize = null,
            Guid? eventId = null) 
            : base(eventId)
        {
            HashAlgorithm = hashAlgorithm;
            FragmentSize = fragmentSize;
            KnownFileInfos = knownFileInfos;
            StoredFragments = storedFragments;
            Endpoints = endpoints;
        }

        public string HashAlgorithm { get; }

        public long? FragmentSize { get; }

        public IDictionary<string, FileMetadata> KnownFileInfos { get; }

        public IDictionary<string, FragmentMetadata> StoredFragments { get; }

        public ISet<Uri> Endpoints { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.ClientJoinRequested, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinRequestedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinRequestedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinRequestedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinRequestedEvent>(json, jsonSerializerOptions);
        }
    }
}

using MQTTnet;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.Text.Json.Serialization;
using MyTorrent.DistributionServices.Events.JsonConverters;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(ClientJoinAcceptedEventJsonConverter))]
    public class ClientJoinAcceptedEvent : EventBase
    {
        public ClientJoinAcceptedEvent(
            string clientIdentifier,
            long fragmentSize,
            IDictionary<string, FileMetadata> addFileInfos,
            IDictionary<string, FragmentMetadata> addFragmentInfos,
            ISet<string> removeFileInfos,
            ISet<string> removeFragmentInfos,
            IDictionary<string, ClientMetadata> clients,
            Guid? eventId = null) 
            : base(eventId)
        {
            ClientIdentifier = clientIdentifier;
            FragmentSize = fragmentSize;
            AddFileInfos = addFileInfos;
            AddFragmentInfos = addFragmentInfos;
            RemoveFileInfos = removeFileInfos;
            RemoveFragmentInfos = removeFragmentInfos;
            Clients = clients;
        }

        public string ClientIdentifier { get; }

        public long FragmentSize { get;}

        public IDictionary<string, FileMetadata> AddFileInfos { get; }

        public IDictionary<string, FragmentMetadata> AddFragmentInfos { get; }

        public ISet<string> RemoveFileInfos { get; }

        public ISet<string> RemoveFragmentInfos { get; }

        public IDictionary<string, ClientMetadata> Clients { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.ClientJoinAccepted, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinAcceptedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinAcceptedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientJoinAcceptedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<ClientJoinAcceptedEvent>(json, jsonSerializerOptions);
        }
    }
}

using MQTTnet;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.Text.Json.Serialization;
using MyTorrent.DistributionServices.Events.JsonConverters;

namespace MyTorrent.DistributionServices.Events
{
    [JsonConverter(typeof(FileInfoPublishedEventJsonConverter))]
    public class FileInfoPublishedEvent : EventBase, IFragmentedFileInfo
    {
        public FileInfoPublishedEvent(
            string hash,
            long size,
            IEnumerable<string> fragmentSequence,
            Guid? eventId = null) 
            : base(eventId)
        {
            Hash = hash;
            Size = size;
            FragmentSequence = fragmentSequence;
        }

        public string Hash { get; }

        public long Size { get; }

        public IEnumerable<string> FragmentSequence { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MqttApplicationMessage BuildMqttMessage(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            return BuildMqttMessageCore(MqttTopics.FileInfoPublished, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileInfoPublishedEvent FromUtf8Bytes(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FileInfoPublishedEvent>(utf8Json, jsonSerializerOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileInfoPublishedEvent FromJsonString(string json, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= DefaultJsonSerializerOptions;
            return JsonSerializer.Deserialize<FileInfoPublishedEvent>(json, jsonSerializerOptions);
        }
    }
}

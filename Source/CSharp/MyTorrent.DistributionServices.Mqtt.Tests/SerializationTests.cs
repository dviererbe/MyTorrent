using MyTorrent.DistributionServices.Events;
using System;
using System.Text.Json;
using Xunit;

namespace MyTorrent.DistributionServices.Mqtt.Tests
{
    public class SerializationTests
    {
        public static readonly JsonSerializerOptions JsonSerializerOptions_WithoutIgnoreNullValues =
            new JsonSerializerOptions()
            {
                WriteIndented = false,
                IgnoreNullValues = false,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

        public static readonly JsonSerializerOptions JsonSerializerOptions_WithIgnoreNullValues =
            new JsonSerializerOptions()
            {
                WriteIndented = false,
                IgnoreNullValues = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
    }
}

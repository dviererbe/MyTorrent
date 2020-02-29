using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Metadata
{
    public class FileMetadataTests
    {
        private readonly ITestOutputHelper Output;
        private readonly FileMetadata Example;
        private const string JsonString_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"Size\":1024,\"FragmentSequence\":[\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\"]}";
        private const string JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"Size\":1024,\"FragmentSequence\":[\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\"]}";

        public FileMetadataTests(ITestOutputHelper output)
        {
            Output = output;

            HashSet<string> fragmentSequence = new HashSet<string>
            {
                "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069"
            };

            Example = new FileMetadata(size: 1024, fragmentSequence);
        }

        [Fact]
        public void Should_SerializeExpectedJsonString_WhithJsonSerializerOptions_WithIgnoreNullValues()
        {
            string jsonString = JsonSerializer.Serialize(Example, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, jsonString);
        }

        [Fact]
        public void Should_SerializeExpectedJsonString_WhithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            string jsonString = JsonSerializer.Serialize(Example, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, jsonString);
        }

        [Fact]
        public void Should_DeserializeJsonStringCorrectly_WhithJsonSerializerOptions_WithIgnoreNullValues()
        {
            FileMetadata fileMetadata = JsonSerializer.Deserialize<FileMetadata>(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example.Size, fileMetadata.Size);
            Assert.Equal(Example.FragmentSequence, fileMetadata.FragmentSequence);
        }

        [Fact]
        public void Should_DeserializeJsonStringCorrectly_WhithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            FileMetadata fileMetadata = JsonSerializer.Deserialize<FileMetadata>(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example.Size, fileMetadata.Size);
            Assert.Equal(Example.FragmentSequence, fileMetadata.FragmentSequence);
        }
    }
}

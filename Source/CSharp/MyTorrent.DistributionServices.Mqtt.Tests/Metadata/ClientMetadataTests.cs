using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Metadata
{
    public class ClientMetadataTests
    {
        private readonly ITestOutputHelper Output;
        private readonly ClientMetadata Example;
        private const string JsonString_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"Endpoints\":[\"grpc://187.159.123.24:50051\"],\"StoredFragments\":[\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\"]}";
        private const string JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"Endpoints\":[\"grpc://187.159.123.24:50051\"],\"StoredFragments\":[\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\"]}";

        public ClientMetadataTests(ITestOutputHelper output)
        {
            Output = output;

            HashSet<Uri> endpoints = new HashSet<Uri>
            {
                new Uri("grpc://187.159.123.24:50051")
            };

            HashSet<string> storedFragments = new HashSet<string>
            {
                "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069"
            };

            Example = new ClientMetadata(endpoints, storedFragments);
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
            string jsonString = JsonSerializer.Serialize(Example, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, jsonString);
        }

        [Fact]
        public void Should_DeserializeJsonStringCorrectly_WhithJsonSerializerOptions_WithIgnoreNullValues()
        {
            ClientMetadata clientMetadata = JsonSerializer.Deserialize<ClientMetadata>(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example.Endpoints, clientMetadata.Endpoints);
            Assert.Equal(Example.StoredFragments, clientMetadata.StoredFragments);
        }

        [Fact]
        public void Should_DeserializeJsonStringCorrectly_WhithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientMetadata clientMetadata = JsonSerializer.Deserialize<ClientMetadata>(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example.Endpoints, clientMetadata.Endpoints);
            Assert.Equal(Example.StoredFragments, clientMetadata.StoredFragments);
        }
    }
}

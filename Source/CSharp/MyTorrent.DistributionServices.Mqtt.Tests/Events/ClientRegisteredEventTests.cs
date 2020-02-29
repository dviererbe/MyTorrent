using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Events
{
    public class ClientRegisteredEventTests
    {
        private readonly ITestOutputHelper Output;
        private readonly ClientRegisteredEvent Example;
        private const string JsonString_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier\",\"Info\":{\"Endpoints\":[\"grpc://10.243.138.183:50051\"],\"StoredFragments\":[\"86BE9A55762D316A3026C2836D44F5FC76E34DA10E1B45FEEE5F18BE7EDB177\"]},\"AddedFiles\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"AddedFragments\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}}}";
        private const string JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier\",\"Info\":{\"Endpoints\":[\"grpc://10.243.138.183:50051\"],\"StoredFragments\":[\"86BE9A55762D316A3026C2836D44F5FC76E34DA10E1B45FEEE5F18BE7EDB177\"]},\"AddedFiles\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"AddedFragments\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}}}";

        public ClientRegisteredEventTests(ITestOutputHelper output)
        {
            Output = output;

            Example = new ClientRegisteredEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                clientIdentifier: "ClientIdentifier",
                info: new ClientMetadata(
                        endpoints: new HashSet<Uri>() { new Uri("grpc://10.243.138.183:50051") },
                        storedFragments: new HashSet<string>() { "86BE9A55762D316A3026C2836D44F5FC76E34DA10E1B45FEEE5F18BE7EDB177" }),
                addedFiles: new Dictionary<string, FileMetadata>()
                {
                    { "559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD", new FileMetadata(1, new [] { "DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C", "6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D" }) },
                    { "3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43", new FileMetadata(2, new [] { "A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58" }) }
                },
                addedFragments: new Dictionary<string, FragmentMetadata>()
                {
                    { "F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9", new FragmentMetadata(3) }
                });
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            string jsonString = Example.ToJsonString(SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, jsonString);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            string jsonString = Example.ToJsonString(SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, jsonString);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            ClientRegisteredEvent clientRegisteredEvent = ClientRegisteredEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example.EventId, clientRegisteredEvent.EventId);
            Assert.Equal(Example.ClientIdentifier, clientRegisteredEvent.ClientIdentifier);
            Assert.Equal(Example.Info, clientRegisteredEvent.Info);
            Assert.Equal(Example.AddedFiles, clientRegisteredEvent.AddedFiles);
            Assert.Equal(Example.AddedFragments, clientRegisteredEvent.AddedFragments);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientRegisteredEvent clientRegisteredEvent = ClientRegisteredEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example.EventId, clientRegisteredEvent.EventId);
            Assert.Equal(Example.ClientIdentifier, clientRegisteredEvent.ClientIdentifier);
            Assert.Equal(Example.Info, clientRegisteredEvent.Info);
            Assert.Equal(Example.AddedFiles, clientRegisteredEvent.AddedFiles);
            Assert.Equal(Example.AddedFragments, clientRegisteredEvent.AddedFragments);
        }
    }
}

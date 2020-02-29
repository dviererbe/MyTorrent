using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Events
{
    public class ClientJoinRequestedEventTests
    {
        private readonly ITestOutputHelper Output;
        private readonly ClientJoinRequestedEvent Example_WithFragmentSize;
        private readonly ClientJoinRequestedEvent Example_WithoutFragmentSize;
        private const string JsonString_WithFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"FragmentSize\":1024,\"HashAlgorithm\":\"SHA256\",\"KnownFileInfos\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"StoredFragments\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}},\"Endpoints\":[\"grpc://10.243.138.183:50051\"]}";
        private const string JsonString_WithFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"FragmentSize\":1024,\"HashAlgorithm\":\"SHA256\",\"KnownFileInfos\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"StoredFragments\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}},\"Endpoints\":[\"grpc://10.243.138.183:50051\"]}";
        private const string JsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"HashAlgorithm\":\"SHA256\",\"KnownFileInfos\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"StoredFragments\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}},\"Endpoints\":[\"grpc://10.243.138.183:50051\"]}";
        private const string JsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"FragmentSize\":null,\"HashAlgorithm\":\"SHA256\",\"KnownFileInfos\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"StoredFragments\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}},\"Endpoints\":[\"grpc://10.243.138.183:50051\"]}";

        public ClientJoinRequestedEventTests(ITestOutputHelper output)
        {
            Output = output;

            Example_WithFragmentSize = new ClientJoinRequestedEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                hashAlgorithm: "SHA256",
                fragmentSize: 1024,
                knownFileInfos: new Dictionary<string, FileMetadata>()
                {
                    { "559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD", new FileMetadata(1, new [] { "DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C", "6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D" }) },
                    { "3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43", new FileMetadata(2, new [] { "A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58" }) }
                },
                storedFragments: new Dictionary<string, FragmentMetadata>()
                {
                    { "F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9", new FragmentMetadata(3) }
                },
                endpoints: new HashSet<Uri>()
                {
                    new Uri("grpc://10.243.138.183:50051")
                });

            Example_WithoutFragmentSize = new ClientJoinRequestedEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                hashAlgorithm: "SHA256",
                fragmentSize: null,
                knownFileInfos: new Dictionary<string, FileMetadata>()
                {
                    { "559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD", new FileMetadata(1, new [] { "DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C", "6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D" }) },
                    { "3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43", new FileMetadata(2, new [] { "A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58" }) }
                },
                storedFragments: new Dictionary<string, FragmentMetadata>()
                {
                    { "F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9", new FragmentMetadata(3) }
                },
                endpoints: new HashSet<Uri>()
                {
                    new Uri("grpc://10.243.138.183:50051")
                });
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            string jsonString = Example_WithFragmentSize.ToJsonString(SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues, jsonString);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            string jsonString = Example_WithFragmentSize.ToJsonString(SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues, jsonString);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            string jsonString = Example_WithoutFragmentSize.ToJsonString(SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues, jsonString);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            string jsonString = Example_WithoutFragmentSize.ToJsonString(SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues, jsonString);
        }

        [Fact]
        public void FromJsonString_WithFragmentSize_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            ClientJoinRequestedEvent clientJoinRequestedEvent = ClientJoinRequestedEvent.FromJsonString(JsonString_WithFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example_WithFragmentSize.EventId, clientJoinRequestedEvent.EventId);
            Assert.Equal(Example_WithFragmentSize.HashAlgorithm, clientJoinRequestedEvent.HashAlgorithm);
            Assert.Equal(Example_WithFragmentSize.FragmentSize, clientJoinRequestedEvent.FragmentSize);
            Assert.Equal(Example_WithFragmentSize.KnownFileInfos, clientJoinRequestedEvent.KnownFileInfos);
            Assert.Equal(Example_WithFragmentSize.StoredFragments, clientJoinRequestedEvent.StoredFragments);
            Assert.Equal(Example_WithFragmentSize.Endpoints, clientJoinRequestedEvent.Endpoints);
        }

        [Fact]
        public void FromJsonString_WithFragmentSize_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientJoinRequestedEvent clientJoinRequestedEvent = ClientJoinRequestedEvent.FromJsonString(JsonString_WithFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example_WithFragmentSize.EventId, clientJoinRequestedEvent.EventId);
            Assert.Equal(Example_WithFragmentSize.HashAlgorithm, clientJoinRequestedEvent.HashAlgorithm);
            Assert.Equal(Example_WithFragmentSize.FragmentSize, clientJoinRequestedEvent.FragmentSize);
            Assert.Equal(Example_WithFragmentSize.KnownFileInfos, clientJoinRequestedEvent.KnownFileInfos);
            Assert.Equal(Example_WithFragmentSize.StoredFragments, clientJoinRequestedEvent.StoredFragments);
            Assert.Equal(Example_WithFragmentSize.Endpoints, clientJoinRequestedEvent.Endpoints);
        }

        [Fact]
        public void FromJsonString_WithoutFragmentSize_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            ClientJoinRequestedEvent clientJoinRequestedEvent = ClientJoinRequestedEvent.FromJsonString(JsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example_WithoutFragmentSize.EventId, clientJoinRequestedEvent.EventId);
            Assert.Equal(Example_WithoutFragmentSize.HashAlgorithm, clientJoinRequestedEvent.HashAlgorithm);
            Assert.Equal(Example_WithoutFragmentSize.FragmentSize, clientJoinRequestedEvent.FragmentSize);
            Assert.Equal(Example_WithoutFragmentSize.KnownFileInfos, clientJoinRequestedEvent.KnownFileInfos);
            Assert.Equal(Example_WithoutFragmentSize.StoredFragments, clientJoinRequestedEvent.StoredFragments);
            Assert.Equal(Example_WithoutFragmentSize.Endpoints, clientJoinRequestedEvent.Endpoints);
        }

        [Fact]
        public void FromJsonString_WithoutFragmentSize_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientJoinRequestedEvent clientJoinRequestedEvent = ClientJoinRequestedEvent.FromJsonString(JsonString_WithoutFragmentSize_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example_WithoutFragmentSize.EventId, clientJoinRequestedEvent.EventId);
            Assert.Equal(Example_WithoutFragmentSize.HashAlgorithm, clientJoinRequestedEvent.HashAlgorithm);
            Assert.Equal(Example_WithoutFragmentSize.FragmentSize, clientJoinRequestedEvent.FragmentSize);
            Assert.Equal(Example_WithoutFragmentSize.KnownFileInfos, clientJoinRequestedEvent.KnownFileInfos);
            Assert.Equal(Example_WithoutFragmentSize.StoredFragments, clientJoinRequestedEvent.StoredFragments);
            Assert.Equal(Example_WithoutFragmentSize.Endpoints, clientJoinRequestedEvent.Endpoints);
        }
    }
}

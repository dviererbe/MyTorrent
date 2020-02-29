using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Events
{
    public class ClientJoinAcceptedEventTests
    {
        private readonly ITestOutputHelper Output;
        private readonly ClientJoinAcceptedEvent Example;
        private const string JsonString_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier1\",\"FragmentSize\":1024,\"AddFileInfos\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"AddFragmentInfos\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}},\"RemoveFileInfos\":[\"333E0A1E27815D0CEEE55C473FE3DC93D56C63E3BEE2B3B4AEE8EED6D70191A3\",\"44BD7AE6F478FAE1061E11A7739F4B94D1DAF917982D33B6FC8A01A63F89C21\"],\"RemoveFragmentInfos\":[\"A83DD0CCBFFE39D071CC317DDF6E97F5C6B1C87AF91919271F9FA140B0508C6C\",\"6DA43B944E494E885E69AF21F93C6D9331C78AA22808471142916A5BBD15B5\"],\"Clients\":{\"ClientIdentifier2\":{\"Endpoints\":[\"grpc://10.243.138.183:50051\"],\"StoredFragments\":[\"86BE9A55762D316A3026C2836D44F5FC76E34DA10E1B45FEEE5F18BE7EDB177\"]}}}";
        private const string JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier1\",\"FragmentSize\":1024,\"AddFileInfos\":{\"559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD\":{\"Size\":1,\"FragmentSequence\":[\"DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C\",\"6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D\"]},\"3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43\":{\"Size\":2,\"FragmentSequence\":[\"A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58\"]}},\"AddFragmentInfos\":{\"F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9\":{\"Size\":3}},\"RemoveFileInfos\":[\"333E0A1E27815D0CEEE55C473FE3DC93D56C63E3BEE2B3B4AEE8EED6D70191A3\",\"44BD7AE6F478FAE1061E11A7739F4B94D1DAF917982D33B6FC8A01A63F89C21\"],\"RemoveFragmentInfos\":[\"A83DD0CCBFFE39D071CC317DDF6E97F5C6B1C87AF91919271F9FA140B0508C6C\",\"6DA43B944E494E885E69AF21F93C6D9331C78AA22808471142916A5BBD15B5\"],\"Clients\":{\"ClientIdentifier2\":{\"Endpoints\":[\"grpc://10.243.138.183:50051\"],\"StoredFragments\":[\"86BE9A55762D316A3026C2836D44F5FC76E34DA10E1B45FEEE5F18BE7EDB177\"]}}}";

        public ClientJoinAcceptedEventTests(ITestOutputHelper output)
        {
            Output = output;
            Example = new ClientJoinAcceptedEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                fragmentSize: 1024,
                clientIdentifier: "ClientIdentifier1",
                addFileInfos: new Dictionary<string, FileMetadata>()
                {
                    { "559AEAD08264D5795D3909718CDD05ABD49572E84FE55590EEF31A88A08FDFFD", new FileMetadata(1, new [] { "DF7E70E521544F4834BBEE64A9E3789FEBC4BE81470DF629CAD6DDB3320A5C", "6B23C0D5F35D1B11F9B683F0B0A617355DEB11277D91AE091D399C655B87940D" }) },
                    { "3F39D5C348E5B79D6E842C114E6CC571583BBF44E4B0EBFDA1A01EC5745D43", new FileMetadata(2, new [] { "A9F51566BD6705F7EA6AD54BB9DEB449F795582D6529A0E22207B8981233EC58" }) }
                },
                addFragmentInfos: new Dictionary<string, FragmentMetadata>()
                {
                    { "F67AB10AD4E4C53121B6A5FE4DA9C10DDEE905B978D3788D2723D7BFACBE28A9", new FragmentMetadata(3) }
                }, 
                removeFileInfos: new HashSet<string>()
                {
                    "333E0A1E27815D0CEEE55C473FE3DC93D56C63E3BEE2B3B4AEE8EED6D70191A3",
                    "44BD7AE6F478FAE1061E11A7739F4B94D1DAF917982D33B6FC8A01A63F89C21"
                },
                removeFragmentInfos: new HashSet<string>()
                {
                    "A83DD0CCBFFE39D071CC317DDF6E97F5C6B1C87AF91919271F9FA140B0508C6C",
                    "6DA43B944E494E885E69AF21F93C6D9331C78AA22808471142916A5BBD15B5"
                },
                clients: new Dictionary<string, ClientMetadata>()
                {
                    { "ClientIdentifier2", new ClientMetadata(
                        endpoints: new HashSet<Uri>() { new Uri("grpc://10.243.138.183:50051") }, 
                        storedFragments:  new HashSet<string>() { "86BE9A55762D316A3026C2836D44F5FC76E34DA10E1B45FEEE5F18BE7EDB177" }) }
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
            ClientJoinAcceptedEvent clientJoinAcceptedEvent = ClientJoinAcceptedEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example.EventId, clientJoinAcceptedEvent.EventId);
            Assert.Equal(Example.ClientIdentifier, clientJoinAcceptedEvent.ClientIdentifier);
            Assert.Equal(Example.FragmentSize, clientJoinAcceptedEvent.FragmentSize);
            Assert.Equal(Example.AddFileInfos, clientJoinAcceptedEvent.AddFileInfos);
            Assert.Equal(Example.AddFragmentInfos, clientJoinAcceptedEvent.AddFragmentInfos);
            Assert.Equal(Example.RemoveFileInfos, clientJoinAcceptedEvent.RemoveFileInfos);
            Assert.Equal(Example.RemoveFragmentInfos, clientJoinAcceptedEvent.RemoveFragmentInfos);
            Assert.Equal(Example.Clients, clientJoinAcceptedEvent.Clients);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientJoinAcceptedEvent clientJoinAcceptedEvent = ClientJoinAcceptedEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example.EventId, clientJoinAcceptedEvent.EventId);
            Assert.Equal(Example.ClientIdentifier, clientJoinAcceptedEvent.ClientIdentifier);
            Assert.Equal(Example.FragmentSize, clientJoinAcceptedEvent.FragmentSize);
            Assert.Equal(Example.AddFileInfos, clientJoinAcceptedEvent.AddFileInfos);
            Assert.Equal(Example.AddFragmentInfos, clientJoinAcceptedEvent.AddFragmentInfos);
            Assert.Equal(Example.RemoveFileInfos, clientJoinAcceptedEvent.RemoveFileInfos);
            Assert.Equal(Example.RemoveFragmentInfos, clientJoinAcceptedEvent.RemoveFragmentInfos);
            Assert.Equal(Example.Clients, clientJoinAcceptedEvent.Clients);
        }
    }
}

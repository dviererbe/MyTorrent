using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Events
{
    public class FileInfoPublishedEventTests
    {
        private readonly ITestOutputHelper Output;
        private readonly FileInfoPublishedEvent Example;
        private const string JsonString_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"Hash\":\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\",\"Size\":1024,\"FragmentSequence\":[\"11FF46BE634069DBD303A12357D8648B42A60696B81F7BAFECED8B1EE50CB\",\"9A2CFA3A7045A553D62C13D7BDBA75135BF9E1B194CA915BCB0A0C0DDFC0F3F\"]}";
        private const string JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"Hash\":\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\",\"Size\":1024,\"FragmentSequence\":[\"11FF46BE634069DBD303A12357D8648B42A60696B81F7BAFECED8B1EE50CB\",\"9A2CFA3A7045A553D62C13D7BDBA75135BF9E1B194CA915BCB0A0C0DDFC0F3F\"]}";

        public FileInfoPublishedEventTests(ITestOutputHelper output)
        {
            Output = output;
            Example = new FileInfoPublishedEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                hash: "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
                size: 1024,
                fragmentSequence: new [] 
                { 
                    "11FF46BE634069DBD303A12357D8648B42A60696B81F7BAFECED8B1EE50CB",
                    "9A2CFA3A7045A553D62C13D7BDBA75135BF9E1B194CA915BCB0A0C0DDFC0F3F"
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
            TrackerGoodbyeEvent trackerGoodbyeEvent = TrackerGoodbyeEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example.EventId, trackerGoodbyeEvent.EventId);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            TrackerGoodbyeEvent trackerGoodbyeEvent = TrackerGoodbyeEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example.EventId, trackerGoodbyeEvent.EventId);
        }
    }
}

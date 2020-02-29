using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Events
{
    public class ClientJoinFailedEventTests
    {
        private readonly ITestOutputHelper Output;
        private readonly ClientJoinFailedEvent Example;
        private const string JsonString_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\"}";
        private const string JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\"}";

        public ClientJoinFailedEventTests(ITestOutputHelper output)
        {
            Output = output;
            Example = new ClientJoinFailedEvent(eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"));
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
            ClientJoinFailedEvent clientJoinDeniedEvent = ClientJoinFailedEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example.EventId, clientJoinDeniedEvent.EventId);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientJoinFailedEvent clientJoinDeniedEvent = ClientJoinFailedEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example.EventId, clientJoinDeniedEvent.EventId);
        }
    }
}

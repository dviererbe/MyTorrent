using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Events
{
    public class ClientJoinDeniedEventTests
    {
        private readonly ITestOutputHelper Output;
        private readonly ClientJoinDeniedEvent Example_WithReason;
        private readonly ClientJoinDeniedEvent Example_WithoutReason;
        private const string JsonString_WithReason_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier\",\"ReasonCode\":128,\"Reason\":\"Reason\"}";
        private const string JsonString_WithReason_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier\",\"ReasonCode\":128,\"Reason\":\"Reason\"}";
        private const string JsonString_WithoutReason_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier\",\"ReasonCode\":1}";
        private const string JsonString_WithoutReason_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"ClientIdentifier\":\"ClientIdentifier\",\"ReasonCode\":1,\"Reason\":null}";

        public ClientJoinDeniedEventTests(ITestOutputHelper output)
        {
            Output = output;
            Example_WithReason = new ClientJoinDeniedEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                clientIdentifier: "ClientIdentifier",
                reasonCode: ClientJoinDeniedCode.Other, 
                reason: "Reason");
            Example_WithoutReason = new ClientJoinDeniedEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                clientIdentifier: "ClientIdentifier",
                reasonCode: ClientJoinDeniedCode.WrongFragmentSize,
                reason: null);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithReason_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            string jsonString = Example_WithReason.ToJsonString(SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithReason_WithJsonSerializerOptions_WithIgnoreNullValues, jsonString);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithoutReason_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            string jsonString = Example_WithoutReason.ToJsonString(SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithoutReason_WithJsonSerializerOptions_WithoutIgnoreNullValues, jsonString);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithoutReason_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            string jsonString = Example_WithoutReason.ToJsonString(SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithoutReason_WithJsonSerializerOptions_WithIgnoreNullValues, jsonString);
        }

        [Fact]
        public void ToJsonString_Should_ReturnExpectedJsonString_WithReason_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            string jsonString = Example_WithReason.ToJsonString(SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);
            Output.WriteLine("JsonString: " + jsonString);

            Assert.Equal(JsonString_WithReason_WithJsonSerializerOptions_WithoutIgnoreNullValues, jsonString);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithReason_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            ClientJoinDeniedEvent clientJoinDeniedEvent = ClientJoinDeniedEvent.FromJsonString(JsonString_WithReason_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example_WithReason.EventId, clientJoinDeniedEvent.EventId);
            Assert.Equal(Example_WithReason.ClientIdentifier, clientJoinDeniedEvent.ClientIdentifier);
            Assert.Equal(Example_WithReason.ReasonCode, clientJoinDeniedEvent.ReasonCode);
            Assert.Equal(Example_WithReason.Reason, clientJoinDeniedEvent.Reason);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithReason_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientJoinDeniedEvent clientJoinDeniedEvent = ClientJoinDeniedEvent.FromJsonString(JsonString_WithReason_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example_WithReason.EventId, clientJoinDeniedEvent.EventId);
            Assert.Equal(Example_WithReason.ClientIdentifier, clientJoinDeniedEvent.ClientIdentifier);
            Assert.Equal(Example_WithReason.ReasonCode, clientJoinDeniedEvent.ReasonCode);
            Assert.Equal(Example_WithReason.Reason, clientJoinDeniedEvent.Reason);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithoutReason_WithJsonSerializerOptions_WithIgnoreNullValues()
        {
            ClientJoinDeniedEvent clientJoinDeniedEvent = ClientJoinDeniedEvent.FromJsonString(JsonString_WithoutReason_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example_WithoutReason.EventId, clientJoinDeniedEvent.EventId);
            Assert.Equal(Example_WithoutReason.ClientIdentifier, clientJoinDeniedEvent.ClientIdentifier);
            Assert.Equal(Example_WithoutReason.ReasonCode, clientJoinDeniedEvent.ReasonCode);
            Assert.Equal(Example_WithoutReason.Reason, clientJoinDeniedEvent.Reason);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithoutReason_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            ClientJoinDeniedEvent clientJoinDeniedEvent = ClientJoinDeniedEvent.FromJsonString(JsonString_WithoutReason_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example_WithoutReason.EventId, clientJoinDeniedEvent.EventId);
            Assert.Equal(Example_WithoutReason.ClientIdentifier, clientJoinDeniedEvent.ClientIdentifier);
            Assert.Equal(Example_WithoutReason.ReasonCode, clientJoinDeniedEvent.ReasonCode);
            Assert.Equal(Example_WithoutReason.Reason, clientJoinDeniedEvent.Reason);
        }
    }
}

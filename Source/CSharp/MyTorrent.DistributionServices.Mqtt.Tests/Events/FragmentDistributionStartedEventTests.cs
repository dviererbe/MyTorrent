using MyTorrent.DistributionServices.Events;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyTorrent.DistributionServices.Mqtt.Tests.Events
{
    public class FragmentDistributionStartedEventTests
    {
        private readonly ITestOutputHelper Output;
        private readonly FragmentDistributionStartedEvent Example;
        private const string JsonString_WithJsonSerializerOptions_WithIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"Hash\":\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\",\"Size\":12}";
        private const string JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues = "{\"EventId\":\"fb3f146d-c74e-4c75-a3e7-1e768b2e986f\",\"Hash\":\"7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069\",\"Size\":12}";

        public FragmentDistributionStartedEventTests(ITestOutputHelper output)
        {
            Output = output;
            Example = new FragmentDistributionStartedEvent(
                eventId: Guid.ParseExact("fb3f146dc74e4c75a3e71e768b2e986f", "N"),
                hash: "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069",
                size: 12); ;
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
            FragmentDistributionStartedEvent fragmentDistributionStartedEvent = FragmentDistributionStartedEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithIgnoreNullValues);

            Assert.Equal(Example.EventId, fragmentDistributionStartedEvent.EventId);
            Assert.Equal(Example.Hash, fragmentDistributionStartedEvent.Hash);
            Assert.Equal(Example.Size, fragmentDistributionStartedEvent.Size);
        }

        [Fact]
        public void FromJsonString_Should_DeserializeJsonStringCorrectly_WithJsonSerializerOptions_WithoutIgnoreNullValues()
        {
            FragmentDistributionStartedEvent fragmentDistributionStartedEvent = FragmentDistributionStartedEvent.FromJsonString(JsonString_WithJsonSerializerOptions_WithoutIgnoreNullValues, SerializationTests.JsonSerializerOptions_WithoutIgnoreNullValues);

            Assert.Equal(Example.EventId, fragmentDistributionStartedEvent.EventId);
            Assert.Equal(Example.Hash, fragmentDistributionStartedEvent.Hash);
            Assert.Equal(Example.Size, fragmentDistributionStartedEvent.Size);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EventIdCreationSource.Tests
{
    public class CoreEventIdCreationSourceTests : AbstractEventIdCreationSourceTests
    {
        protected override IEventIdCreationSource CreateInstance() => CreateInstance(0);

        protected IEventIdCreationSource CreateInstance(int initialId)
        {
            return new Microsoft.Extensions.Logging.EventIdCreationSource(initialId);
        }


        [Theory]
        [InlineData(new object[] { 0, null })]
        [InlineData(new object[] { 1, null })]
        [InlineData(new object[] { 0, "" })]
        [InlineData(new object[] { 1, "" })]
        [InlineData(new object[] { 0, "EventName" })]
        [InlineData(new object[] { 1, "EventName" })]
        public void ShouldReturnEventIdWithInitialId(int initialId, string eventName)
        {
            IEventIdCreationSource eventIdCreationSource = CreateInstance(initialId);
            EventId eventId = eventIdCreationSource.GetNextId(eventName);

            Assert.Equal(initialId, eventId.Id);
            Assert.Equal(eventName, eventId.Name);
        }

        [Theory]
        [InlineData(new object[] { 0, null })]
        [InlineData(new object[] { 1, null })]
        [InlineData(new object[] { 0, "" })]
        [InlineData(new object[] { 1, "" })]
        [InlineData(new object[] { 0, "EventName" })]
        [InlineData(new object[] { 1, "EventName" })]
        public void ShouldReturnEventIdsWithIncrementalIdValues(int initialId, string eventName)
        {
            IEventIdCreationSource eventIdCreationSource = CreateInstance(initialId);
            
            foreach (int id in Enumerable.Range(initialId, 100))
            {
                EventId eventId = eventIdCreationSource.GetNextId(eventName);

                Assert.Equal(id, eventId.Id);
                Assert.Equal(eventName, eventId.Name);
            }
        }
    }
}

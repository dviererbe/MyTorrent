using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EventIdCreationSource.Tests
{
    public abstract class AbstractEventIdCreationSourceTests
    {
        protected abstract IEventIdCreationSource CreateInstance();

        [Theory]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "" })]
        [InlineData(new object[] { "EventName" })]
        public void ShouldCreateUniqueIds_WithSameEventName(string eventName)
        {
            IEventIdCreationSource eventIdCreationSource = CreateInstance();

            HashSet<int> ids = new HashSet<int>();

            foreach (int i in Enumerable.Range(0, 100))
            {
                EventId eventId = eventIdCreationSource.GetNextId(eventName);

                Assert.True(ids.Add(eventId.Id));
                Assert.Equal(eventName, eventId.Name);
            }
        }

        [Fact]
        public void ShouldCreateUniqueIds_WithDifferentEventName()
        {
            IEventIdCreationSource eventIdCreationSource = CreateInstance();

            HashSet<int> ids = new HashSet<int>();

            string eventNameTemplate = "EventName{0}";

            foreach (int i in Enumerable.Range(0, 100))
            {
                string eventName = string.Format(eventNameTemplate, i);
                EventId eventId = eventIdCreationSource.GetNextId(eventName);

                Assert.True(ids.Add(eventId.Id));
                Assert.Equal(eventName, eventId.Name);
            }
        }
    }
}

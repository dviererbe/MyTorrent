using System.Threading;

namespace Microsoft.Extensions.Logging
{

    /// <summary>
    /// <see cref="IEventIdCreationSource"/> implementation that uses an threadsafe counter.
    /// </summary>
    public class EventIdCreationSource : IEventIdCreationSource
    {
        private int _counter;

        /// <summary>
        /// Initializes a new <see cref="EventIdCreationSource"/> instance.
        /// </summary>
        /// <param name="initialId">
        /// The first event id returned by this <see cref="EventIdCreationSource"/> instance.
        /// </param>
        public EventIdCreationSource(int initialId = 0)
        {
            //Subtract 1 because for the next id will be added 1.
            _counter = initialId - 1;
        }

        /// <summary>
        /// Returns the next unique event id.
        /// </summary>
        /// <returns>
        /// The next unique event id.
        /// </returns>
        public EventId GetNextId(string? name = null)
        {
            //increment counter threadsafe and return incremented result
            return new EventId(Interlocked.Increment(ref _counter), name);
        }
    }
}

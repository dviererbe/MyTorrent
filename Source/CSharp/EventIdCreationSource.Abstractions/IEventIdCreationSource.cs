using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Creation source for unique event Id's.
    /// </summary>
    public interface IEventIdCreationSource
    {
        /// <summary>
        /// Returns the next unique event id.
        /// </summary>
        /// <param name="name">
        /// The name of the created event.
        /// </param>
        /// <returns>
        /// The next unique event id.
        /// </returns>
        EventId GetNextId(string? name = null);
    }
}

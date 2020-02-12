using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="EventIdCreationSource"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class EventIdCreationSourceExtension
    {
        /// <summary>
        /// Adds a transient <see cref="IEventIdCreationSource"/> service that uses the <see cref="EventIdCreationSource"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the service to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddEventIdCreationSourceCore(this IServiceCollection services)
        {
            return services.AddTransient<IEventIdCreationSource, EventIdCreationSource>();
        }
    }
}

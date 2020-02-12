using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="PhysicalManagedFragmentFileStorageProvider"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class PhysicalManagedFragmentFileStorageProviderExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="PhysicalManagedFragmentFileStorageProviderOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="PhysicalManagedFragmentFileStorageProviderOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureFragmentInMemoryStorageProvider(this IServiceCollection services, Action<PhysicalManagedFragmentFileStorageProviderOptions> setupAction)
        {
            return services.Configure<PhysicalManagedFragmentFileStorageProviderOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="PhysicalManagedFragmentFileStorageProviderOptions"/> will bind against.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the configuration to.
        /// </param>
        /// <param name="configuration">
        /// The configuration that should be used.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureFragmentInMemoryStorageProvider(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<PhysicalManagedFragmentFileStorageProviderOptions>(configuration);
        }

        /// <summary>
        /// Adds a singleton <see cref="IFragmentStorageProvider"/> service that uses the <see cref="PhysicalManagedFragmentFileStorageProvider"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the service to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddFragmentInMemoryStorageProvider(this IServiceCollection services)
        {
            return services.AddSingleton<IFragmentStorageProvider, PhysicalManagedFragmentFileStorageProvider>();
        }
    }
}
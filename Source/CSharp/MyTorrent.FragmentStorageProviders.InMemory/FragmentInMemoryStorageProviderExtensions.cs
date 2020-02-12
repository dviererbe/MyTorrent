using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.FragmentStorageProviders
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="FragmentInMemoryStorageProvider"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class FragmentInMemoryStorageProviderExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="FragmentInMemoryStorageProviderOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="FragmentInMemoryStorageProviderOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureFragmentInMemoryStorageProvider(this IServiceCollection services, Action<FragmentInMemoryStorageProviderOptions> setupAction)
        {
            return services.Configure<FragmentInMemoryStorageProviderOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="FragmentInMemoryStorageProviderOptions"/> will bind against.
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
            return services.Configure<FragmentInMemoryStorageProviderOptions>(configuration);
        }

        /// <summary>
        /// Adds a transient <see cref="IFragmentStorageProvider"/> service that uses the <see cref="FragmentInMemoryStorageProvider"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the service to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddFragmentInMemoryStorageProvider(this IServiceCollection services)
        {
            return services.AddSingleton<IFragmentStorageProvider, FragmentInMemoryStorageProvider>();
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.HashingServiceProviders
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="StandardHashingServiceProvider"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class StandardHashingServiceProviderExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="StandardHashingServiceProviderOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="StandardHashingServiceProviderOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureStandardHashingServiceProvider(this IServiceCollection services, Action<StandardHashingServiceProviderOptions> setupAction)
        {
            return services.Configure<StandardHashingServiceProviderOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="StandardHashingServiceProviderOptions"/> will bind against.
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
        public static IServiceCollection ConfigureStandardHashingServiceProvider(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<StandardHashingServiceProviderOptions>(configuration);
        }

        /// <summary>
        /// Adds a singleton <see cref="IHashingServiceProvider"/> service that uses the <see cref="StandardHashingServiceProvider"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <see cref="StandardHashingServiceProvider"/> to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddStandardHashingServiceProvider(this IServiceCollection services)
        {
            return services.AddSingleton<IHashingServiceProvider, StandardHashingServiceProvider>();
        }
    }
}

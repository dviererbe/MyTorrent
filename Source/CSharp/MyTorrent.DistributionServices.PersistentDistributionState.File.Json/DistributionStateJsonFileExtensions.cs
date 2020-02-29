using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="DistributionStateJsonFile"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class DistributionStateJsonFileExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="DistributionStateJsonFileOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="DistributionStateJsonFileOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureDistributionStateJsonFile(this IServiceCollection services, Action<DistributionStateJsonFileOptions> setupAction)
        {
            return services.Configure<DistributionStateJsonFileOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="DistributionStateJsonFileOptions"/> will bind against.
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
        public static IServiceCollection ConfigureDistributionStateJsonFile(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<DistributionStateJsonFileOptions>(configuration);
        }

        /// <summary>
        /// Adds a singleton <see cref="IPersistentDistributionState"/> service that uses the <see cref="DistributionStateJsonFile"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <see cref="DistributionStateJsonFile"/> to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddDistributionStateJsonFile(this IServiceCollection services)
        {
            return services.AddSingleton<IPersistentDistributionState, DistributionStateJsonFile>();
        }
    }
}
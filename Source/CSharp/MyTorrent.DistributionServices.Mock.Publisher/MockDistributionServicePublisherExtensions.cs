using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="MockDistributionServicePublisher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class MockDistributionServicePublisherExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="MockDistributionServicePublisherOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="MockDistributionServicePublisherOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureMockDistributionServicePublisher(this IServiceCollection services, Action<MockDistributionServicePublisherOptions> setupAction)
        {
            return services.Configure<MockDistributionServicePublisherOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="MockDistributionServicePublisherOptions"/> will bind against.
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
        public static IServiceCollection ConfigureMockDistributionServicePublisher(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<MockDistributionServicePublisherOptions>(configuration);
        }

        /// <summary>
        /// Adds a singleton <see cref="IDistributionServicePublisher"/> service that uses the <see cref="MockDistributionServicePublisher"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <see cref="MockDistributionServicePublisher"/> to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddMockDistributionServicePublisher(this IServiceCollection services)
        {
            return services.AddSingleton<IDistributionServicePublisher, MockDistributionServicePublisher>();
        }
    }
}

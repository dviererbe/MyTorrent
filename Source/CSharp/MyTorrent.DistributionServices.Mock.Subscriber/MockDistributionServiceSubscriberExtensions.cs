using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="MockDistributionServiceSubscriber"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class MockDistributionServiceSubscriberExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="MockDistributionServiceSubscriberOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="MockDistributionServiceSubscriberOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureMockDistributionServiceSubscriber(this IServiceCollection services, Action<MockDistributionServiceSubscriberOptions> setupAction)
        {
            return services.Configure<MockDistributionServiceSubscriberOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="MockDistributionServiceSubscriberOptions"/> will bind against.
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
        public static IServiceCollection ConfigureMockDistributionServiceSubscriber(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<MockDistributionServiceSubscriberOptions>(configuration);
        }

        /// <summary>
        /// Adds a singleton <see cref="IDistributionServicePublisher"/> service that uses the <see cref="MockDistributionServiceSubscriber"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <see cref="MockDistributionServiceSubscriber"/> to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddMockDistributionServiceSubscriber(this IServiceCollection services)
        {
            return services.AddSingleton<IDistributionServiceSubscriber, MockDistributionServiceSubscriber>();
        }
    }
}

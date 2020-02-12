using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="MqttDistributionServicePublisher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class MqttDistributionServicePublisherExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="MqttDistributionServicePublisherOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="MqttDistributionServicePublisherOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureMqttDistributionServicePublisher(this IServiceCollection services, Action<MqttDistributionServicePublisherOptions> setupAction)
        {
            return services.Configure<MqttDistributionServicePublisherOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="MqttDistributionServicePublisherOptions"/> will bind against.
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
        public static IServiceCollection ConfigureMqttDistributionServicePublisher(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<MqttDistributionServicePublisherOptions>(configuration);
        }

        /// <summary>
        /// Adds a singleton <see cref="IDistributionServicePublisher"/> service that uses the <see cref="MqttDistributionServicePublisher"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <see cref="MqttDistributionServicePublisher"/> to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddMqttDistributionServicePublisher(this IServiceCollection services)
        {
            return services.AddSingleton<IDistributionServicePublisher, MqttDistributionServicePublisher>();
        }
    }
}

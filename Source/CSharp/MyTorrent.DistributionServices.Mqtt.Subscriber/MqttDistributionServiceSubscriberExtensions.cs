using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="MqttDistributionServiceSubscriber"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class MqttDistributionServiceSubscriberExtensions
    {
        /// <summary>
        /// Registers an action used to configure <see cref="MqttDistributionServiceSubscriberOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="MqttDistributionServiceSubscriberOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureMqttDistributionServiceSubscriber(this IServiceCollection services, Action<MqttDistributionServiceSubscriberOptions> setupAction)
        {
            return services.Configure <MqttDistributionServiceSubscriberOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="MqttDistributionServiceSubscriberOptions"/> will bind against.
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
        public static IServiceCollection ConfigureMqttDistributionServiceSubscriber(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<MqttDistributionServiceSubscriberOptions>(configuration);
        }

        /// <summary>
        /// Adds a singleton <see cref="IDistributionServicePublisher"/> service that uses the <see cref="MqttDistributionServiceSubscriber"/> implementation.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <see cref="MqttDistributionServiceSubscriber"/> to.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection AddMqttDistributionServiceSubscriber(this IServiceCollection services)
        {
            return services.AddSingleton<IDistributionServiceSubscriber, MqttDistributionServiceSubscriber>();
        }
    }
}

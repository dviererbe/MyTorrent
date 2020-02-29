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
        /// Registers an action used to configure <see cref="MqttNetworkOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="MqttNetworkOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureMqttNetwork(this IServiceCollection services, Action<MqttNetworkOptions> setupAction)
        {
            return services.Configure<MqttNetworkOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="MqttNetworkOptions"/> will bind against.
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
        public static IServiceCollection ConfigureMqttNetwork(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<MqttNetworkOptions>(configuration);
        }

        /// <summary>
        /// Registers an action used to configure <see cref="DistributionServiceSubscriberOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="DistributionServiceSubscriberOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureMqttDistributionServiceSubscriber(this IServiceCollection services, Action<DistributionServiceSubscriberOptions> setupAction)
        {
            return services.Configure<DistributionServiceSubscriberOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="DistributionServiceSubscriberOptions"/> will bind against.
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
        public static IServiceCollection ConfigureMqttDistributionServiceSubscriber(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.Configure<DistributionServiceSubscriberOptions>(configuration);
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
            return services.AddSingleton<IMqttEndpoint, RemoteMqttBroker>()
                           .AddSingleton<IDistributionServiceSubscriber, MqttDistributionServiceSubscriber>();

            
        }
    }
}

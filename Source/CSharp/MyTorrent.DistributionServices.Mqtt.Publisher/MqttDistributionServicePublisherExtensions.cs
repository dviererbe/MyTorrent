using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// Extension methods for configuring and adding <see cref="MqttDistributionServicePublisher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class MqttDistributionServicePublisherExtensions
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
        /// Registers an action used to configure <see cref="DistributionServicePublisherOptions"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add the <paramref name="setupAction"/> to.
        /// </param>
        /// <param name="setupAction">
        /// The action used to to configure <see cref="DistributionServicePublisherOptions"/>.
        /// </param>
        /// <returns>
        /// Returns <paramref name="services"/> after the operation has completed.
        /// </returns>
        public static IServiceCollection ConfigureMqttDistributionServicePublisher(this IServiceCollection services, Action<DistributionServicePublisherOptions> setupAction)
        {
            return services.Configure<DistributionServicePublisherOptions>(setupAction);
        }

        /// <summary>
        /// Registers a <see cref="IConfiguration"/> instance wich <see cref="DistributionServicePublisherOptions"/> will bind against.
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
            return services.Configure<DistributionServicePublisherOptions>(configuration);
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
            services.AddSingleton<IMqttEndpoint>(serviceProvider =>
            {
                IOptions<MqttNetworkOptions>? options = serviceProvider.GetService<IOptions<MqttNetworkOptions>>();
                MqttNetworkOptions mqttNetworkOptions = options?.Value ?? new MqttNetworkOptions();

                IEventIdCreationSource eventIdCreationSource = serviceProvider.GetRequiredService<IEventIdCreationSource>();

                if (mqttNetworkOptions.MqttBrokerType == MqttBrokerType.SelfHosted)
                {
                    ILogger<SelfHostedMqttBroker> logger = serviceProvider.GetRequiredService<ILogger<SelfHostedMqttBroker>>();

                    return new SelfHostedMqttBroker(logger, eventIdCreationSource, mqttNetworkOptions.Port);
                }
                else if (mqttNetworkOptions.MqttBrokerType == MqttBrokerType.Remote)
                {
                    ILogger<RemoteMqttBroker> logger = serviceProvider.GetRequiredService<ILogger<RemoteMqttBroker>>();

                    return new RemoteMqttBroker(logger, eventIdCreationSource, mqttNetworkOptions.Host ?? MqttNetworkOptions.Default.Host!, mqttNetworkOptions.Port);
                }
                else
                {
                    throw new InvalidOperationException("Unknown broker type: " + mqttNetworkOptions.MqttBrokerType);
                }
            });

            return services.AddSingleton<IDistributionServicePublisher, MqttDistributionServicePublisher>();
        }
    }
}

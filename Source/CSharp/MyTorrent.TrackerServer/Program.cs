using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyTorrent.HashingServiceProviders;
using MyTorrent.FragmentStorageProviders;
using MyTorrent.DistributionServices;
using MyTorrent.TrackerServer.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace MyTorrent.TrackerServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (IHost host = BuildHost(args))
            {
                await host.StartAsync();
                await host.WaitForShutdownAsync();
            }
        }

        static IHost BuildHost(string[] args)
        {
            void ConfigureHost(IConfigurationBuilder hostConfiguration)
            {
                hostConfiguration.SetBasePath(Directory.GetCurrentDirectory());
                hostConfiguration.AddJsonFile("hostsettings.json", optional: true);
                hostConfiguration.AddEnvironmentVariables(prefix: "MYTORRENT-TRACKER_");
                hostConfiguration.AddCommandLine(args);
            }

            void ConfigureApplication(HostBuilderContext hostContext, IConfigurationBuilder appConfiguration)
            {
                appConfiguration.AddJsonFile("appsettings.json", optional: true);
                appConfiguration.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                appConfiguration.AddEnvironmentVariables(prefix: "MYTORRENT-TRACKER_");
                appConfiguration.AddCommandLine(args);
            }

            static void ConfigureLogging(HostBuilderContext hostContext, LoggerConfiguration logging)
            {
                logging.ReadFrom.Configuration(hostContext.Configuration);
            }

            static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
            {
                IConfiguration configuration = hostContext.Configuration;

                services.Configure<HostOptions>(option => option.ShutdownTimeout = TimeSpan.FromSeconds(20))
                        .Configure<GrpcTrackerServiceOptions>(configuration.GetSection("gRPC"))
                        .ConfigureStandardHashingServiceProvider(configuration.GetSection("HashServiceProvider"))
                        .ConfigureFragmentInMemoryStorageProvider(configuration.GetSection("Storage:InMemory"))
                        .ConfigureMqttNetwork(configuration.GetSection("Distribution:Mqtt:Network"))
                        .ConfigureMqttNetwork(configuration.GetSection("Distribution"))

                        .AddOptions()
                        .AddEventIdCreationSourceCore()
                        .AddStandardHashingServiceProvider()
                        .AddFragmentInMemoryStorageProvider()
                        .AddMqttDistributionServicePublisher()
                        .AddHostedService<GrpcTrackerService>();
            }

            return new HostBuilder()
                .ConfigureHostConfiguration(ConfigureHost)
                .ConfigureAppConfiguration(ConfigureApplication)
                .UseSerilog(ConfigureLogging)
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime()
                .Build();
        }
    }
}

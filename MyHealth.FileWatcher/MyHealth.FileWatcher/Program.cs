using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyHealth.Common;
using MyHealth.FileWatcher.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyHealth.FileWatcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting MyHealth.FileWatcher");
            await StartListening();
            Console.WriteLine("MyHealth.FileWatcher completed!");
        }

        private static async Task StartListening()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration
                (
                    (hostBuilderContext, configurationBuilder) =>
                    {
                        configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
                        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    }
                )
                .ConfigureServices
                (
                    (hostBuilderContext, services) =>
                    {
                        services.AddHostedService<HostedService>();
                        services.AddTransient<IFileWatcherService, FileWatcherService>();
                        services.AddTransient<IAzureBlobHelpers, AzureBlobHelpers>();
                        services.AddTransient<IServiceBusHelpers, ServiceBusHelpers>();
                        services.AddSingleton(sp =>
                            new BlobContainerClient(hostBuilderContext.Configuration["StorageConnectionString"], hostBuilderContext.Configuration["ContainerName"]));
                        services.AddSingleton(sp =>
                            new ServiceBusClient(hostBuilderContext.Configuration["ServiceBusConnectionString"]));
                    }
                );

            await hostBuilder.RunConsoleAsync();
        }
    }
}

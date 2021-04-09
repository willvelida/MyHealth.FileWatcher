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
                        services.AddSingleton(sp =>
                            new AzureBlobHelpers(hostBuilderContext.Configuration["StorageConnectionString"], hostBuilderContext.Configuration["ContainerName"]));
                        services.AddSingleton(sp =>
                            new ServiceBusHelpers(hostBuilderContext.Configuration["ServiceBusConnectionString"]));
                    }
                );

            await hostBuilder.RunConsoleAsync();
        }
    }
}

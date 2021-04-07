using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyHealth.FileWatcher.Services
{
    public class HostedService : IHostedService
    {
        private readonly IFileWatcherService _fileWatcherService;

        public HostedService(
            IFileWatcherService fileWatcherService)
        {
            _fileWatcherService = fileWatcherService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("StartAsync");
            Task.Run(() => _fileWatcherService.StartListening());

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("StopAsync");
            Task.Run(() => _fileWatcherService.StopListening());

            return Task.CompletedTask;
        }
    }
}

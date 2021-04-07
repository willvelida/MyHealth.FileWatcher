using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using MyHealth.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace MyHealth.FileWatcher.Services
{
    public class FileWatcherService : IFileWatcherService
    {
        private readonly Timer _pollTimer;
        private readonly IConfiguration _configuration;
        private readonly IAzureBlobHelpers _azureBlobHelpers;
        private readonly IServiceBusHelpers _serviceBusHelpers;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ServiceBusClient _serviceBusClient;

        private readonly int _secondsBetweenPolls;

        public FileWatcherService(
            IConfiguration configuration,
            IAzureBlobHelpers azureBlobHelpers,
            IServiceBusHelpers serviceBusHelpers,
            BlobContainerClient blobContainerClient,
            ServiceBusClient serviceBusClient)
        {
            _pollTimer = new Timer();
            _configuration = configuration;
            _azureBlobHelpers = azureBlobHelpers;
            _serviceBusHelpers = serviceBusHelpers;
            _blobContainerClient = blobContainerClient;
            _serviceBusClient = serviceBusClient;
            _secondsBetweenPolls = int.Parse(_configuration["SecondsBetweenPolls"]);
        }

        public async Task StartListening()
        {
            try
            {
                StartFileWatcher();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartListening Exception: {ex.Message}");
                await _serviceBusHelpers.SendMessageToTopic(_serviceBusClient, _configuration["ExceptionTopicName"], ex);
            }
        }

        public void StopListening()
        {
            _pollTimer.Dispose();
        }

        private void StartFileWatcher()
        {
            _pollTimer.AutoReset = false;
            _pollTimer.Elapsed += new ElapsedEventHandler(PollDirectoryForFileAsync);
            _pollTimer.Interval = 1;
            _pollTimer.Start();
        }

        private async void PollDirectoryForFileAsync(object sender, ElapsedEventArgs e)
        {
            try
            {
                var onPremFilePaths = Directory.EnumerateFiles(_configuration["LocalDirectoryPath"]);

                if (!onPremFilePaths.Any())
                {
                    _pollTimer.Interval = _secondsBetweenPolls * 1000;
                    _pollTimer.Start();
                    return;
                }

                foreach (var file in onPremFilePaths)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileExtension = Path.GetExtension(file);
                    string fullFileName;

                    if (fileName.StartsWith("activity"))
                    {
                        fullFileName = "activity/" + fileName + fileExtension;
                    }
                    else if (fileName.StartsWith("sleep"))
                    {
                        fullFileName = "sleep/" + fileName + fileExtension;
                    }
                    else
                    {
                        throw new Exception("File has invalid format");
                    }

                    await _azureBlobHelpers.UploadBlobAsync(_blobContainerClient, fullFileName, file);
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown during PollDirectoryForFile execution: {ex.Message}");
                await _serviceBusHelpers.SendMessageToTopic(_serviceBusClient, _configuration["ExceptionTopicName"], ex);
            }
        }
    }
}

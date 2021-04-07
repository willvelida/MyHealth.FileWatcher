using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using MyHealth.Common;
using System;
using System.IO;
using System.Linq;
using System.Timers;

namespace MyHealth.FileWatcher.Services
{
    public class FileWatcherService : IFileWatcherService
    {
        private readonly Timer _pollTimer;
        private readonly IConfiguration _configuration;
        private readonly IAzureBlobHelpers _azureBlobHelpers;
        private readonly BlobContainerClient _blobContainerClient;

        private readonly int _secondsBetweenPolls;

        public FileWatcherService(
            IConfiguration configuration,
            IAzureBlobHelpers azureBlobHelpers,
            BlobContainerClient blobContainerClient)
        {
            _pollTimer = new Timer();
            _configuration = configuration;
            _azureBlobHelpers = azureBlobHelpers;
            _blobContainerClient = blobContainerClient;
            _secondsBetweenPolls = int.Parse(_configuration["SecondsBetweenPolls"]);
        }

        public void StartListening()
        {
            try
            {
                StartFileWatcher();
            }
            catch (Exception ex)
            {
                // TODO Write to Serilog or write to exception queue in service bus
                Console.WriteLine($"StartListening Exception: {ex.Message}");
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
            }
        }
    }
}

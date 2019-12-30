using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace ClientConsumer
{
    class PartitionReceiver : IPartitionReceiveHandler
    {
        readonly CloudBlobClient _cloudBlobClient = null;
        readonly string _downloadDestination = string.Empty;
        int _maxBatchSize = 1;
        public int MaxBatchSize { get => _maxBatchSize; set => _maxBatchSize = value; }

        public PartitionReceiver(CloudStorageAccount storageAccount, string downloadDestination)
        {
            _cloudBlobClient = storageAccount.CreateCloudBlobClient();
            _downloadDestination = downloadDestination;
        }

        public Task ProcessErrorAsync(Exception error)
        {
            Console.WriteLine(error.Message);
            return Task.FromException(error);
        }

        public async Task ProcessEventsAsync(IEnumerable<EventData> events)
        {
            foreach(var e in events)
            {
                string body = Encoding.UTF8.GetString(e.Body);

                var jsonMessage = JArray.Parse(body).First;

                Uri uploadedUri = new Uri(jsonMessage["data"]["url"].ToString());
                Console.WriteLine("Blob available at: {0}", uploadedUri);
                var cloudBlob = _cloudBlobClient.GetBlobReferenceFromServer(uploadedUri);

                string uploadedFile = Path.GetFileName(jsonMessage["data"]["url"].ToString());
                string destinationFile = Path.Combine(_downloadDestination, Path.GetFileName(uploadedFile));
                Console.WriteLine("Downloading to {0}...", destinationFile);
                await cloudBlob.DownloadToFileAsync(destinationFile, FileMode.Create);
                Console.WriteLine("Done.");
                Console.WriteLine();
            }
        }
    }

    class EventHubsConsumer : IConsumer
    {
        private string downloadDestination;

        private CloudStorageAccount storageAccount;

        private EventHubClient client;

        public void Configure()
        {
            Console.WriteLine("Validating settings...");
            foreach (string option in new string[] { "EventHubConnectionString", "StorageConnectionString", "DownloadDestination" })
            {
                if (string.IsNullOrEmpty(ConfigurationManager.AppSettings?[option]))
                {
                    Console.WriteLine($"Missing '{option}' in App.config.");
                    return;
                }
            }

            string storageConnectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
            string eventhubConnectionString = ConfigurationManager.AppSettings["EventHubConnectionString"];

            downloadDestination = ConfigurationManager.AppSettings["DownloadDestination"];

            Console.WriteLine("Connecting to Storage account...");

            storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            Console.WriteLine("Connecting to EventHub...");
            
            client = EventHubClient.CreateFromConnectionString(eventhubConnectionString);
        }

        public async Task ProcessMessages(CancellationToken cancellationToken)
        {
            Console.WriteLine("The application will now start to listen for incoming message.");

            var runtimeInfo = await client.GetRuntimeInformationAsync();
            Console.WriteLine("Creating receiver handlers...");
            var utcNow = DateTime.UtcNow;
            var receivers = runtimeInfo.PartitionIds
                .Select(pid => {
                    var receiver = client.CreateReceiver("$Default", pid, EventPosition.FromEnqueuedTime(utcNow));
                    Console.WriteLine("Created receiver for partition '{0}'.", pid);
                    receiver.SetReceiveHandler(new PartitionReceiver(storageAccount, downloadDestination));
                    return receiver;
                })
                .ToList();

            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // This is okay because the task was cancelled. :)
            }
            finally
            {
                // Clean up nicely.
                await Task.WhenAll(
                    receivers.Select(receiver => receiver.CloseAsync())
                );
            }
        }
    }
}
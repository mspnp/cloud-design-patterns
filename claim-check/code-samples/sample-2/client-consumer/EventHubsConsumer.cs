using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Newtonsoft.Json.Linq;

namespace ClientConsumer
{
    class EventHubsConsumer : IConsumer
    {
        private String downloadDestination;
       
        private EventProcessorClient processor;

        public void Configure()
        {
            Console.WriteLine("Validating settings...");
            foreach (string option in new string[] { "EventHubConnectionString", "StorageConnectionString", "blobContainerName", "DownloadDestination" })
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
            string blobContainerName = ConfigurationManager.AppSettings["BlobContainerName"];
            Console.WriteLine("Connecting to Storage account...");
            BlobContainerClient blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
            Console.WriteLine("Connecting to EventHub...");
            processor = new EventProcessorClient(blobContainerClient, EventHubConsumerClient.DefaultConsumerGroupName, eventhubConnectionString);
        }

        public async Task ProcessMessages(CancellationToken cancellationToken)
        {
            Console.WriteLine("The application will now start to listen for incoming message.");
            int eventIndex = 0;

            Task processEventHandlerAsync(ProcessEventArgs eventArgs)
            {
                if (eventArgs.CancellationToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }
                try
                {
                    ++eventIndex;
                    Console.WriteLine($"Event Received: { Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()) }");
                    string body = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
                    var jsonMessage = JArray.Parse(body).First;
                    Uri uploadedUri = new Uri(jsonMessage["data"]["url"].ToString());
                    Console.WriteLine("Blob available at: {0}", uploadedUri);
                    BlockBlobClient blockBlob = new BlockBlobClient(uploadedUri);
                    string uploadedFile = Path.GetFileName(jsonMessage["data"]["url"].ToString());
                    string destinationFile = Path.Combine(downloadDestination, Path.GetFileName(uploadedFile));
                    Console.WriteLine("Downloading to {0}...", destinationFile);
                    blockBlob.DownloadTo(destinationFile);
                    Console.WriteLine("Done.");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error was observed while processing events.  Message: { ex.Message }");
                }
                return Task.CompletedTask;
            };

            Task processErrorHandler(ProcessErrorEventArgs eventArgs)
            {
                if (eventArgs.CancellationToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }
                Console.WriteLine();
                Console.WriteLine("===============================");
                Console.WriteLine($"The error handler was invoked during the operation: { eventArgs.Operation ?? "Unknown" }, for Exception: { eventArgs.Exception.Message }");
                Console.WriteLine("===============================");
                Console.WriteLine();
                return Task.CompletedTask;
            }
            processor.ProcessEventAsync += processEventHandlerAsync;
            processor.ProcessErrorAsync += processErrorHandler;

            try
            {
                eventIndex = 0;
                await processor.StartProcessingAsync();
                await Task.Delay(-1, cancellationToken);
                await processor.StopProcessingAsync();
            }
            catch (TaskCanceledException)
            {
                // This is okay because the task was cancelled. :)
            }
            finally
            {
                processor.ProcessEventAsync -= processEventHandlerAsync;
                processor.ProcessErrorAsync -= processErrorHandler;
            }
        }
    }
}
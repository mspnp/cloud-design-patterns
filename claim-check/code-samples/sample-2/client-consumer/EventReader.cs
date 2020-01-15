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
    class EventReader : IReader
    {
        private string downloadDestination;
       
        private EventProcessorClient processor;

        public void Configure()
        {            
            string storageConnectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
            string eventhubConnectionString = ConfigurationManager.AppSettings["EventHubConnectionString"];
            downloadDestination = ConfigurationManager.AppSettings["DownloadDestination"];
            string blobContainerName = ConfigurationManager.AppSettings["BlobContainerName"];
            Console.WriteLine("Validating settings...");
            if(string.IsNullOrEmpty(storageConnectionString) | string.IsNullOrEmpty(eventhubConnectionString) | string.IsNullOrEmpty(downloadDestination) | string.IsNullOrEmpty(blobContainerName))
            {
                Console.WriteLine($"Missing AppSetting value in App.config.");
                return;
            }
            Console.WriteLine("Connecting to Storage account...");
            BlobContainerClient blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
            Console.WriteLine("Connecting to EventHub...");
            processor = new EventProcessorClient(blobContainerClient, EventHubConsumerClient.DefaultConsumerGroupName, eventhubConnectionString);
        }

        //As an example, we'll just log the exception to the console in try/catch block.
        //For real-world scenarios, you should take action appropriate to your application.
        public async Task ProcessMessages(CancellationToken cancellationToken)
        {
            Console.WriteLine("The application will now start to listen for incoming messages.");

            Task partitionInitializingHandler(PartitionInitializingEventArgs eventArgs)
            {
                if (eventArgs.CancellationToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }
                try
                {
                    eventArgs.DefaultStartingPosition = EventPosition.FromEnqueuedTime(DateTimeOffset.UtcNow);
                    Console.WriteLine($"Initialized partition: { eventArgs.PartitionId }");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"An error was observed while initializing partition: { eventArgs.PartitionId }.  Message: { ex.Message }");
                    Console.WriteLine();
                }
                return Task.CompletedTask;
            }

            async Task processEventHandlerAsync(ProcessEventArgs eventArgs)
            {
                if (eventArgs.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                try
                {                  
                    string body = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
                    var jsonMessage = JArray.Parse(body).First;
                    Uri uploadedUri = new Uri(jsonMessage["data"]["url"].ToString());
                    Console.WriteLine("Blob available at: {0}", uploadedUri);
                    BlockBlobClient blockBlob = new BlockBlobClient(uploadedUri);
                    string uploadedFile = Path.GetFileName(uploadedUri.ToString());
                    string destinationFile = Path.Combine(downloadDestination, Path.GetFileName(uploadedFile));
                    Console.WriteLine("Downloading to {0}...", destinationFile);
                    await blockBlob.DownloadToAsync(destinationFile);
                    Console.WriteLine("Done.");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error was observed while processing events.  Message: { ex.Message }");
                }             
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
            processor.PartitionInitializingAsync += partitionInitializingHandler;
            processor.ProcessEventAsync += processEventHandlerAsync;
            processor.ProcessErrorAsync += processErrorHandler;

            try
            {
                await processor.StartProcessingAsync();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // This is okay because when pressing any key, it will send a cancellation, then the task was cancelled. :)
            }
            finally
            {
                await processor.StopProcessingAsync();
                processor.PartitionInitializingAsync -= partitionInitializingHandler;
                processor.ProcessEventAsync -= processEventHandlerAsync;
                processor.ProcessErrorAsync -= processErrorHandler;
            }
        }
    }
}
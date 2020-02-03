using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Newtonsoft.Json.Linq;


namespace ClientConsumer
{
    class StorageQueueConsumer : IConsumer
    {
        private QueueClient _queueClient;
        private string _downloadDestination;

        public void Configure()
        {

            foreach (string option in new string[] { "StorageConnectionString", "StorageQueueName", "DownloadDestination" })
            {
                if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings?[option]))
                {
                    throw new ApplicationException($"Missing '{option}' in App.config.");                    
                }
            }

            string storageConnectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
            string storageQueueName = ConfigurationManager.AppSettings["StorageQueueName"];
            _downloadDestination = ConfigurationManager.AppSettings["DownloadDestination"];

            Console.WriteLine("Connecting to Azure Storage Account...");
            _queueClient = new QueueClient(storageConnectionString, storageQueueName);
            Console.WriteLine("Connected to {0}.", storageQueueName);
            Console.WriteLine();
        }

        public async Task ProcessMessages(CancellationToken token)
        {
            foreach (QueueMessage message in (await _queueClient.ReceiveMessagesAsync(maxMessages: 10)).Value)
            {
                var jsonMessage = JObject.Parse(message.MessageText);
                Uri uploadedUri = new Uri(jsonMessage["data"]["url"].ToString());
                string uploadedFile = Path.GetFileName(jsonMessage["data"]["url"].ToString());
                Console.WriteLine("Blob available at: {0}", jsonMessage["data"]["url"]);

                BlockBlobClient blockBlob = new BlockBlobClient(uploadedUri);

                string destinationFile = Path.Combine(_downloadDestination, Path.GetFileName(uploadedFile));
                Console.WriteLine("Downloading to {0}...", destinationFile);
                await blockBlob.DownloadToAsync(destinationFile);

                Console.WriteLine("Done.");
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                Console.WriteLine();
            }

            if (!token.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }
    }
}
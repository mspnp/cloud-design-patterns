using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ClientConsumer
{
    class StorageQueueConsumer : IConsumer
    {
        private CloudStorageAccount storageAccount;
        private CloudQueueClient queueClient;
        private CloudQueue queue;
        private CloudBlobClient cloudBlobClient;
        private string downloadDestination;

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
            downloadDestination = ConfigurationManager.AppSettings["DownloadDestination"];

            Console.WriteLine("Connecting to Azure Storage Account...");
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            cloudBlobClient = storageAccount.CreateCloudBlobClient();
            queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference(storageQueueName);
            Console.WriteLine("Connected to {0}.", storageAccount.QueueEndpoint);
            Console.WriteLine();
        }

        public async Task ProcessMessages(CancellationToken token)
        {
            foreach (CloudQueueMessage message in await queue.GetMessagesAsync(1, TimeSpan.FromMinutes(1), null, null))
            {
                var jsonMessage = JObject.Parse(message.AsString);
                Uri uploadedUri = new Uri(jsonMessage["data"]["url"].ToString());
                string uploadedFile = Path.GetFileName(jsonMessage["data"]["url"].ToString());
                Console.WriteLine("Blob available at: {0}", jsonMessage["data"]["url"]);

                var cloudBlob = await cloudBlobClient.GetBlobReferenceFromServerAsync(uploadedUri);

                string destinationFile = Path.Combine(downloadDestination, Path.GetFileName(uploadedFile));
                Console.WriteLine("Downloading to {0}...", destinationFile);
                await cloudBlob.DownloadToFileAsync(destinationFile, FileMode.Create);

                Console.WriteLine("Done.");
                await queue.DeleteMessageAsync(message);
                Console.WriteLine();
            }

            await Task.Delay(1000);
        }
    }
}
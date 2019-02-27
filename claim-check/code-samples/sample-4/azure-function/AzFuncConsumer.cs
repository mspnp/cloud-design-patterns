using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace azure_function
{
    public static class AzFuncConsumer
    {
        [FunctionName("AzFuncConsumer")]
        public static async Task RunAsync([EventHubTrigger("%EVENTHUB_NAME%", Connection = "EventHubConnectionAppSetting")]string myEventHubMessage, ILogger log)
        {
            // Processing  and extracting the notification on Event Hub 
            PayloadDetails payloadDetails = JsonConvert.DeserializeObject<PayloadDetails>(myEventHubMessage);
            string containerName = payloadDetails.ContainerName;
            string blobName = payloadDetails.BlobName;

            // Downloading the message from blob storage for processing
            string storageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // Get reference to the container
                    cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

                    // Get reference to the blob with heavy payload
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

                    log.LogInformation("Here is the large payload information");

                    // Reading payload
                    Stream blobStream = await cloudBlockBlob.OpenReadAsync();
                    using (StreamReader reader = new StreamReader(blobStream))
                    {
                        string data = reader.ReadToEnd();
                        log.LogInformation(data);
                    }
                    // The large payload can be processed further

                }
                catch (StorageException ex)
                {
                    log.LogError("Error returned from the service: {0}", ex.Message);
                }
            }
        }
    }
}

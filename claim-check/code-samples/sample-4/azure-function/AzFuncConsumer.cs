using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

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

                try
                {
                    BlobContainerClient container = new BlobContainerClient(storageConnectionString, containerName);

                    // Get a reference to a blob named "sample-file" in a container named "sample-container"
                    BlobClient blob = container.GetBlobClient(blobName);

                    log.LogInformation("Here is the large payload information");

                    // Reading payload
                    BlobDownloadInfo download =await blob.DownloadAsync();
                    using (StreamReader reader = new StreamReader(download.Content))
                    {
                        string data = reader.ReadToEnd();
                        log.LogInformation(data);
                    }
                    // The large payload can be processed further

                }
                catch (RequestFailedException ex)
                {
                    log.LogError("Error returned from the service: {0}", ex.Message);
                }
            
        }
    }
}

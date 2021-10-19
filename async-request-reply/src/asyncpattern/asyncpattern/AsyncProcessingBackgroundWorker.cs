using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace asyncpattern
{
    public static class AsyncProcessingBackgroundWorker
    {
        [FunctionName("AsyncProcessingBackgroundWorker")]
        public static async Task Run(
            [ServiceBusTrigger("outqueue", Connection = "ServiceBusConnectionAppSetting")] Message myQueueItem,
            [Blob("data", FileAccess.ReadWrite, Connection = "StorageConnectionAppSetting")] BlobContainerClient blobContainer,
            ILogger log)
        {
            // Perform an actual action against the blob data source for the async readers to be able to check against.
            // This is where your actual service worker processing will be performed

            var id = myQueueItem.UserProperties["RequestGUID"] as string;

            var cbb = blobContainer.GetBlobClient($"{id}.blobdata");

            // Now write away the process 
            MemoryStream stream = new MemoryStream();
            stream.Write(myQueueItem.Body, 0, myQueueItem.Body.Length);
            stream.Position = 0;
            BlobHttpHeaders blobHttpHeaders = new BlobHttpHeaders();
            blobHttpHeaders.ContentType = "application/json";
            await cbb.UploadAsync(stream, blobHttpHeaders);
        }
    }
}

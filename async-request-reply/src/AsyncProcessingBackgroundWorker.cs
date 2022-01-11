using System.IO;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace Contoso
{
    public static class AsyncProcessingBackgroundWorker
    {
        [FunctionName("AsyncProcessingBackgroundWorker")]
        public static void Run(
            [ServiceBusTrigger("outqueue", Connection = "ServiceBusConnectionAppSetting")] ServiceBusMessage myQueueItem,
            [Blob("data", FileAccess.ReadWrite, Connection = "StorageConnectionAppSetting")] BlobContainerClient inputBlob,
            ILogger log)
        {
            // Perform an actual action against the blob data source for the async readers to be able to check against.
            // This is where your actual service worker processing will be performed

            var id = myQueueItem.ApplicationProperties["RequestGUID"] as string;

            BlobClient cbb = inputBlob.GetBlobClient($"{id}.blobdata");

            // Now write away the process 
            cbb.UploadAsync(myQueueItem.Body);
        }
    }
}

using System.IO;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Contoso
{
    public static class AsyncProcessingBackgroundWorker
    {
        [FunctionName("AsyncProcessingBackgroundWorker")]
        public static void Run(
            [ServiceBusTrigger("outqueue", Connection = "ServiceBusConnectionAppSetting")]ServiceBusMessage myQueueItem,
            [Blob("data", FileAccess.ReadWrite, Connection = "StorageConnectionAppSetting")] CloudBlobContainer inputBlob,
            ILogger log)
        {
            // Perform an actual action against the blob data source for the async readers to be able to check against.
            // This is where your actual service worker processing will be performed

            var id = myQueueItem.ApplicationProperties["RequestGUID"] as string;

            CloudBlockBlob cbb = inputBlob.GetBlockBlobReference($"{id}.blobdata");

            // Now write away the process 
            cbb.UploadFromByteArrayAsync(myQueueItem.Body.ToArray(), 0, myQueueItem.Body.ToArray().Length);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Contoso
{
    public static class AsyncProcessingBackgroundWorker
    {
        [FunctionName("AsyncProcessingBackgroundWorker")]
        public static async Task RunAsync(
            [ServiceBusTrigger("outqueue", Connection = "ServiceBusConnectionAppSetting")] BinaryData customer,
            IDictionary<string, object> applicationProperties,
            [Blob("data", FileAccess.ReadWrite, Connection = "StorageConnectionAppSetting")] BlobContainerClient inputContainer,
            ILogger log)
        {
            // Perform an actual action against the blob data source for the async readers to be able to check against.
            // This is where your actual service worker processing will be performed

            var id = applicationProperties["RequestGUID"] as string;
            
            BlobClient blob = inputContainer.GetBlobClient($"{id}.blobdata");

            // Now write away the process 
            await blob.UploadAsync(customer);
        }
    }
}

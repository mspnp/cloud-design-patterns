using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace asyncpattern
{
    public class AsyncProcessingBackgroundWorker
    {
        private readonly ILogger<AsyncProcessingBackgroundWorker> _logger;

        private readonly BlobServiceClient _blobServiceClient;

        public AsyncProcessingBackgroundWorker(BlobServiceClient blobServiceClient, ILogger<AsyncProcessingBackgroundWorker> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [Function(nameof(AsyncProcessingBackgroundWorker))]
        public async Task Run([ServiceBusTrigger("outqueue", Connection = "ServiceBusConnectionAppSetting")] ServiceBusReceivedMessage message)
        {
            //Perform an actual action against the blob data source for the async readers to be able to check against.
            // This is where your actual service worker processing will be performed

            var requestGuid = message.ApplicationProperties["RequestGUID"].ToString();
            string blobName = $"{requestGuid}.blobdata";
            string containerName = "data";

            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobName);
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(memoryStream))
            {
                writer.Write(message.Body.ToString());
                writer.Flush();
                memoryStream.Position = 0;

                await blobClient.UploadAsync(memoryStream, overwrite: true);
            }
        }
    }
}

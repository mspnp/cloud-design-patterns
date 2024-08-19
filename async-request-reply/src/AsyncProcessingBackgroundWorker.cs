using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace asyncpattern
{
    public class AsyncProcessingBackgroundWorker
    {
        private readonly ILogger<AsyncProcessingBackgroundWorker> _logger;

        private readonly BlobContainerClient _blobContainerClient;

        public AsyncProcessingBackgroundWorker(BlobContainerClient blobContainerClient, ILogger<AsyncProcessingBackgroundWorker> logger)
        {
            _blobContainerClient = blobContainerClient;
            _logger = logger;
        }

        [Function(nameof(AsyncProcessingBackgroundWorker))]
        public async Task Run([ServiceBusTrigger("outqueue", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            //Perform an actual action against the blob data source for the async readers to be able to check against.
            // This is where your actual service worker processing will be performed

            var requestGuid = message.ApplicationProperties["RequestGUID"].ToString();
            string blobName = $"{requestGuid}.blobdata";

            await _blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = _blobContainerClient.GetBlobClient(blobName);
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

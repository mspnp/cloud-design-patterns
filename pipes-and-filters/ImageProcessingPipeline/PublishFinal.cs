using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace ImageProcessingPipeline
{
    public class PublishFinal
    {
        private readonly ILogger<PublishFinal> _logger;
        private readonly BlobContainerClient _destinationContainerClient;

        public PublishFinal(ILogger<PublishFinal> logger, IAzureClientFactory<BlobServiceClient> blobClientFactory)
        {
            _logger = logger;
            _destinationContainerClient = blobClientFactory.CreateClient("processed").GetBlobContainerClient("processed");
            _destinationContainerClient.CreateIfNotExists();
        }

        [Function(nameof(PublishFinal))]
        public async Task RunAsync(
            [QueueTrigger("pipe-yhrb", Connection = "pipe")] string imageFilePath,
            [BlobInput("{QueueTrigger}", Connection = "pipe")] BlockBlobClient imageBlob,
            FunctionContext context)
        {
            _logger.LogDebug("Starting copying {source} into {destination}.", imageBlob.Uri, _destinationContainerClient.Uri);

            // Copy blob
            await _destinationContainerClient.UploadBlobAsync(imageBlob.Name, await imageBlob.OpenReadAsync(null, context.CancellationToken), context.CancellationToken);

            _logger.LogInformation("Copied {source} into {destination}.", imageBlob.Uri, _destinationContainerClient.Uri);
        }
    }
}

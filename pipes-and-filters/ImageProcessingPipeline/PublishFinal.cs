using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace ImageProcessingPipeline
{
    public class PublishFinal(ILogger<PublishFinal> logger, IAzureClientFactory<BlobServiceClient> blobClientFactory)
    {
        private readonly ILogger<PublishFinal> _logger = logger;
        private readonly BlobContainerClient _destinationContainerClient = blobClientFactory.CreateClient("processed").GetBlobContainerClient("processed");

        [Function(nameof(PublishFinal))]
        public async Task RunAsync(
            [QueueTrigger("pipe-yhrb", Connection = "pipe")] string imageFilePath,
            [BlobInput("{QueueTrigger}", Connection = "pipe")] BlockBlobClient imageBlob,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting copying {source} into {destination}.", imageBlob.Uri, _destinationContainerClient.Uri);

            // Copy blob and delete orginal
            var newBlobClient = _destinationContainerClient.GetBlobClient(imageBlob.Name);
            await newBlobClient.UploadAsync(await imageBlob.OpenReadAsync(null, cancellationToken), overwrite: true, cancellationToken);
            await imageBlob.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);

            _logger.LogInformation("Copied {source} into {destination} and deleted original.", imageBlob.Uri, _destinationContainerClient.Uri);
        }
    }
}

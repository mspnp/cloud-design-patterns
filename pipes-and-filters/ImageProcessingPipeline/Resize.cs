using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessingPipeline
{

    public class Resize(ILogger<Resize> logger)
    {
        private readonly ILogger<Resize> _logger = logger;

        [Function(nameof(Resize))]
        [QueueOutput("pipe-fjur", Connection = "pipe")]
        public async Task<string> RunAsync(
            [QueueTrigger("pipe-xfty", Connection = "pipe")] string imageFilePath,
            [BlobInput("{QueueTrigger}", Connection = "pipe")] BlockBlobClient imageBlob,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing image {uri} for resizing.", imageBlob.Uri);

            // Download image and resize it
            using BlobDownloadStreamingResult imageBlobContents = await imageBlob.DownloadStreamingAsync(null, cancellationToken);
            var image = await Image.LoadAsync(imageBlobContents.Content, cancellationToken);
            image.Mutate(i =>
            {
                i.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(600, 600)
                });
            });

            // Write modified image back to storage
            _logger.LogDebug("Writing resized image back to storage: {uri}.", imageBlob.Uri);
            using (var blobStream = await imageBlob.OpenWriteAsync(overwrite: true, null, cancellationToken))
            {
                await image.SaveAsync(blobStream, image.Metadata.DecodedImageFormat!, cancellationToken);
            }

            _logger.LogInformation("Image resizing done. Adding image \"{filePath}\" into the next pipe.", imageFilePath);
            return imageFilePath;
        }
    }
}

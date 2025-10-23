using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessingPipeline
{
    public class Watermark(ILogger<Watermark> logger, IFileProvider files)
    {
        private readonly ILogger<Watermark> _logger = logger;
        private readonly IFileProvider _files = files;

        [Function(nameof(Watermark))]
        [QueueOutput("pipe-yhrb", Connection = "pipe")]
        public async Task<string> RunAsync(
            [QueueTrigger("pipe-fjur", Connection = "pipe")] string imageFilePath,
            [BlobInput("{QueueTrigger}", Connection = "pipe")] BlockBlobClient imageBlob,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing image {uri} for watermarking.", imageBlob.Uri);

            // Download image and watermark it
            using BlobDownloadStreamingResult imageBlobContents = await imageBlob.DownloadStreamingAsync(null, cancellationToken);
            var image = await Image.LoadAsync(imageBlobContents.Content, cancellationToken);

            var resources = _files.GetDirectoryContents("/");
            using var watermarkStream  = resources.First(resource=>resource.Name.Equals("resources/watermark.png")).CreateReadStream();
            var watermarkImage = await Image.LoadAsync(watermarkStream, cancellationToken);

            image.Mutate(i =>
            {
                i.DrawImage(watermarkImage, new Point((image.Width - watermarkImage.Width) / 2, (image.Height - watermarkImage.Height) / 2), 0.5f);
            });

            // Write modified image back to storage
            _logger.LogDebug("Writing watermarked image back to storage: {uri}.", imageBlob.Uri);
            using (var blobStream = await imageBlob.OpenWriteAsync(overwrite: true, null, cancellationToken))
            {
                await image.SaveAsync(blobStream, image.Metadata.DecodedImageFormat!, cancellationToken);
            }

            _logger.LogInformation("Watermarking done. Adding image \"{filePath}\" into the next pipe.", imageFilePath);
            return imageFilePath;
        }
    }
}

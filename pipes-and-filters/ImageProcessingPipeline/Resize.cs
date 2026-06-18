using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ImageProcessingPipeline
{
    public class Resize(ILogger<Resize> logger)
    {
        private readonly ILogger<Resize> _logger = logger;
        private const int MaxDimension = 600;

        [Function(nameof(Resize))]
        [QueueOutput("pipe-fjur", Connection = "pipe")]
        public async Task<string> RunAsync(
            [QueueTrigger("pipe-xfty", Connection = "pipe")] string imageFilePath,
            [BlobInput("{QueueTrigger}", Connection = "pipe")] BlockBlobClient imageBlob,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing image {uri} for resizing.", imageBlob.Uri);

            try
            {
                // Download image into memory (blob streams are not seekable for SkiaSharp)
                using BlobDownloadStreamingResult imageBlobContents = await imageBlob.DownloadStreamingAsync(null, cancellationToken);
                using var memoryStream = new MemoryStream();
                await imageBlobContents.Content.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                using var data = SKData.Create(memoryStream);
                using var original = SKBitmap.Decode(data);

                // Validate image decode succeeded
                if (original is null || original.Width <= 0 || original.Height <= 0)
                {
                    _logger.LogError("Failed to decode image {filePath}: invalid or unsupported image", imageFilePath);
                    throw new InvalidOperationException($"Image decode failed or image is empty: {imageFilePath}");
                }

                // Calculate resize scale (constrain to MaxDimension, don't upscale)
                float scale = Math.Min(
                    (float)MaxDimension / original.Width,
                    (float)MaxDimension / original.Height);
                scale = Math.Min(scale, 1.0f); // Don't upscale

                int newWidth = (int)(original.Width * scale);
                int newHeight = (int)(original.Height * scale);

                _logger.LogDebug("Resizing image from {originalWidth}x{originalHeight} to {newWidth}x{newHeight}", original.Width, original.Height, newWidth, newHeight);

                // Resize and encode
                using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
                using var resizedImage = SKImage.FromBitmap(resized);
                using var encoded = resizedImage.Encode(SKEncodedImageFormat.Png, 100);

                // Write back to blob storage
                _logger.LogDebug("Writing resized image back to storage: {uri}.", imageBlob.Uri);
                using (var blobStream = await imageBlob.OpenWriteAsync(overwrite: true, cancellationToken: cancellationToken))
                {
                    encoded.SaveTo(blobStream);
                }

                _logger.LogInformation("Image resizing done. Adding image \"{filePath}\" into the next pipe.", imageFilePath);
                return imageFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resizing image {filePath}: {message}", imageFilePath, ex.Message);
                throw; // Let the queue retry mechanism handle retries
            }
        }
    }
}

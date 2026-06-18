using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ImageProcessingPipeline
{
    public class Watermark(ILogger<Watermark> logger)
    {
        private readonly ILogger<Watermark> _logger = logger;
        private const byte WatermarkAlpha = 128; // 50% opacity

        [Function(nameof(Watermark))]
        [QueueOutput("pipe-yhrb", Connection = "pipe")]
        public async Task<string> RunAsync(
            [QueueTrigger("pipe-fjur", Connection = "pipe")] string imageFilePath,
            [BlobInput("{QueueTrigger}", Connection = "pipe")] BlockBlobClient imageBlob,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing image {uri} for watermarking.", imageBlob.Uri);

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
                if (original.Width <= 0 || original.Height <= 0)
                {
                    _logger.LogError("Failed to decode image {filePath}: invalid dimensions {width}x{height}", imageFilePath, original.Width, original.Height);
                    throw new InvalidOperationException($"Image decode failed or image is empty: {imageFilePath}");
                }

                // Load watermark from resources directory
                var watermarkPath = Path.Combine(AppContext.BaseDirectory, "resources", "watermark.png");
                if (!File.Exists(watermarkPath))
                {
                    _logger.LogError("Watermark file not found at {path}", watermarkPath);
                    throw new FileNotFoundException($"Watermark file not found: {watermarkPath}");
                }

                using var watermarkBitmap = SKBitmap.Decode(watermarkPath);

                // Validate watermark fits in image
                if (watermarkBitmap.Width > original.Width || watermarkBitmap.Height > original.Height)
                {
                    _logger.LogWarning("Watermark {watermarkWidth}x{watermarkHeight} is larger than image {imageWidth}x{imageHeight}. Centering anyway.",
                        watermarkBitmap.Width, watermarkBitmap.Height, original.Width, original.Height);
                }

                // Draw original image and overlay watermark at 50% opacity
                using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
                var canvas = surface.Canvas;
                canvas.DrawBitmap(original, 0, 0);

                // Create watermark with transparency
                int wmX = (original.Width - watermarkBitmap.Width) / 2;
                int wmY = (original.Height - watermarkBitmap.Height) / 2;
                using var wmPaint = new SKPaint { Color = SKColors.White.WithAlpha(WatermarkAlpha) };
                canvas.DrawBitmap(watermarkBitmap, wmX, wmY, wmPaint);
                canvas.Flush();

                // Encode and write back to storage
                _logger.LogDebug("Writing watermarked image back to storage: {uri}.", imageBlob.Uri);
                using var resultImage = surface.Snapshot();
                using var encoded = resultImage.Encode(SKEncodedImageFormat.Png, 100);
                using (var blobStream = await imageBlob.OpenWriteAsync(overwrite: true, cancellationToken: cancellationToken))
                {
                    encoded.SaveTo(blobStream);
                }

                _logger.LogInformation("Watermarking done. Adding image \"{filePath}\" into the next pipe.", imageFilePath);
                return imageFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error watermarking image {filePath}: {message}", imageFilePath, ex.Message);
                throw; // Let the queue retry mechanism handle retries
            }
        }
    }
}

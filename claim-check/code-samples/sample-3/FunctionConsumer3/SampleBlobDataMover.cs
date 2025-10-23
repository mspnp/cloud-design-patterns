using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// A sample Storage Blob uploader/downloader. Uses Azure Entra ID for authentication.
    /// </summary>
    public class SampleBlobDataMover(ILoggerFactory loggerFactory) : ISampleBlobDataMover
    {
        readonly ILogger _logger = loggerFactory.CreateLogger<SampleBlobDataMover>();

        /// <summary>
        ///  Downloads the referenced payload from Azure Blobs and returns the content. 
        ///  Optionally deletes the blob after download
        /// </summary>
        public async Task<string> DownloadAsync(Uri blobUri, bool deleteAfter = true)
        {
            var blobClient = new BlockBlobClient(blobUri, new DefaultAzureCredential());
            if (!await blobClient.ExistsAsync())
            {
                _logger.LogError("Blob {BlobUri} does not exist.", blobUri.AbsoluteUri);
                return string.Empty;
            }
            var response = await blobClient.DownloadContentAsync();
            if (deleteAfter)
            {
                await blobClient.DeleteIfExistsAsync();
            }
            return Encoding.UTF8.GetString(response.Value.Content);
        }

        /// <summary>
        /// Upload sample text content to Azure Blob Storage and returns Url to the newly created blob
        /// </summary>
        public async Task<string> UploadAsync(string blobUri, string containerName, string content)
        {
            var serviceClient = new BlobServiceClient(new Uri(blobUri), new DefaultAzureCredential());

            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString());
            await blobClient.UploadAsync(new BinaryData(content), overwrite: true);
            _logger.LogInformation("Uploaded content to {Uri}", blobClient.Uri.AbsoluteUri);
            return blobClient.Uri.AbsoluteUri;
        }
    }
}
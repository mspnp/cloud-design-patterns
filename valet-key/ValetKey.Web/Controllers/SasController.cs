using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;

namespace ValetKey.Web.Controllers
{

    [ApiController]
    public class SasController : ControllerBase
    {
        private readonly Uri _blobEndpoint;
        private readonly ILogger<SasController> _logger;

        public SasController(IConfiguration configuration, ILogger<SasController> logger)
        {
            _blobEndpoint = new Uri(configuration.GetSection("AppSettings:BlobEndpoint").Value!);
            _logger = logger;
        }

        // This route would typically require authorization
        [HttpGet("api/sas")]
        public async Task<StorageEntitySas> Get()
        {
            try
            {
                var blobName = Guid.NewGuid();

                // Retrieve a shared access signature of the location we should upload this file to
                var blobSas = await this.GetSharedAccessReferenceForUpload(blobName.ToString());

                _logger.LogInformation("Blob Uri: {uri} - Shared Access Signature: {signature}", blobSas.BlobUri, blobSas.Signature);

                return blobSas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generateing SaS.");
                throw;
            }
        }

        /// <summary>
        /// We return a limited access key that allows the caller to upload a file to this specific destination for defined period of time
        /// </summary>
        private async Task<StorageEntitySas> GetSharedAccessReferenceForUpload(string blobName)
        {
            var blobServiceClient = new BlobServiceClient(_blobEndpoint, new DefaultAzureCredential());
            var blobClient = blobServiceClient.GetBlobContainerClient("valetkeysample")
                                              .GetBlockBlobClient(blobName);

            var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                           DateTimeOffset.UtcNow.AddDays(1));

            var blobSasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Write);

            var sas = blobSasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName).ToString();

            return new StorageEntitySas
            {
                BlobUri = blobClient.Uri,
                Signature = sas
            };
        }

        public class StorageEntitySas
        {
            public string? Signature { get; internal set; }
            public Uri? BlobUri { get; internal set; }
        }

    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ValetKey.Web
{
    public class FileServices(ILoggerFactory loggerFactory)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<FileServices>();

        // WARNING: This route would normally require its own AuthZ so that you are handing out valet keys
        //          to only authorized clients. For example, using App Service authentication integrated with
        //          the IdP requirements aligned with your clients.
        [Function(nameof(FileServices))]
        public async Task<StorageEntitySas> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "file-services/access")] HttpRequestData req,
            [BlobInput("uploads", Connection = "UploadStorage")] BlobContainerClient blobContainerClient,
            CancellationToken cancellationToken
            )
        {
            _logger.LogInformation("Processing new request for a valet key.");

            return await GetSharedAccessReferenceForUploadAsync(blobContainerClient, Guid.NewGuid().ToString(), cancellationToken);
        }

        /// <summary>
        /// Return an access key that allows the caller to upload a file to this specific destination for defined period of time (~three minutes).
        /// </summary>
        private async Task<StorageEntitySas> GetSharedAccessReferenceForUploadAsync(BlobContainerClient blobContainerClient, string blobName, CancellationToken cancellationToken)
        {
            var blobServiceClient = blobContainerClient.GetParentBlobServiceClient();
            var blobClient = blobContainerClient.GetBlockBlobClient(blobName);

            var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow.AddMinutes(-3),
                                                                                      DateTimeOffset.UtcNow.AddMinutes(3), cancellationToken);

            // Limit the scope of this SaS token to the following:
            //  - The specific blob
            //  - Create permissions only
            //  - In the next ~three minutes
            //  - Over HTTPs
            var blobSasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobContainerClient.Name,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-3),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(3),
                Protocol = SasProtocol.Https
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Create);

            var sas = blobSasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName).ToString();

            _logger.LogInformation("Generated user delegated SaS token for {uri} that expires at {expiresOn}.", blobClient.Uri, blobSasBuilder.ExpiresOn);

            return new StorageEntitySas
            {
                BlobUri = blobClient.Uri,
                Signature = sas
            };
        }

        public class StorageEntitySas
        {
            public Uri? BlobUri { get; internal set; }
            public string? Signature { get; internal set; }
        }
    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System;

namespace asyncpattern
{
    public static class CloudBlockBlobExtensions
    {
        public static string GenerateSASURI(this BlobClient blobClient)
        {
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b"
            };

            sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
            sasBuilder.SetPermissions(BlobSasPermissions.Read |
                BlobSasPermissions.Write);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            return sasUri.AbsoluteUri;
        }
    }
}

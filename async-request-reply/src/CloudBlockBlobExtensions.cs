using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace asyncpattern
{
    public static class CloudBlockBlobExtensions
    {
        public static string GenerateSASURI(this BlockBlobClient blob)
        {
            BlobSasBuilder blobSasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blob.BlobContainerName,
                BlobName = blob.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Read);

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            Uri sasUri = blob.GenerateSasUri(blobSasBuilder);

            //Return the URI string for the container, including the SAS token.
            return sasUri.ToString();
        }
    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace Asyncpattern
{
    public static class CloudBlockBlobExtensions
    {
        public static string GenerateSASURI(this BlockBlobClient inputBlob, UserDelegationKey userDelegationKey)
        {
            BlobServiceClient blobServiceClient = inputBlob.GetParentBlobContainerClient().GetParentBlobServiceClient();

            BlobSasBuilder blobSasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = inputBlob.BlobContainerName,
                BlobName = inputBlob.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Read);

            var blobUriBuilder = new BlobUriBuilder(inputBlob.Uri)
            {
                Sas = blobSasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
            };

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            Uri sasUri = blobUriBuilder.ToUri();

            //Return the URI string for the container, including the SAS token.
            return sasUri.ToString();
        }
    }
}

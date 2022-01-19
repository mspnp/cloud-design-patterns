using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;


namespace ValetKey.Web.Controllers
{
    [ApiController]
    public class SasController : ControllerBase
    {
        private readonly string blobContainer = "valetkeysample";
        private readonly string blobContainerEndpoint;
        private readonly string blobEndpoint;
        private IConfiguration configuration;


        public SasController(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.blobContainer = configuration.GetSection("AppSettings:ContainerName").Value;
            this.blobEndpoint = configuration.GetSection("AppSettings:BlobEndpoint").Value;
            this.blobContainerEndpoint = $"https://{this.blobEndpoint}/{this.blobContainer}";
        }

        // This route would typically require authorization
        [HttpGet("api/sas")]
        public async Task<string> Get()
        {
            try
            {
                var blobName = Guid.NewGuid();

                // Retrieve a shared access signature of the location we should upload this file to
                var blobSas = await this.GetSharedAccessReferenceForUpload(blobName.ToString());
                Trace.WriteLine(string.Format("Blob Uri: {0} - Shared Access Signature: {1}", blobSas.BlobUri, blobSas.Credentials));

                using (MemoryStream ms = new MemoryStream())
                {
                    new DataContractJsonSerializer(typeof(StorageEntitySas)).WriteObject(ms, blobSas);
                    return Encoding.Default.GetString(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                throw new System.Web.Http.HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("An error has ocurred"),
                    ReasonPhrase = "Critical Exception"
                });
            }
        }

        /// <summary>
        /// We return a limited access key that allows the caller to upload a file to this specific destination for defined period of time
        /// </summary>
        private async Task<StorageEntitySas> GetSharedAccessReferenceForUpload(string blobName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint),
                                                    new DefaultAzureCredential());

            var blobContainerClient = blobServiceClient.GetBlobContainerClient(this.blobContainer);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            BlobServiceClient parentBlobServiceClient = blobContainerClient.GetParentBlobServiceClient();

                UserDelegationKey key = await parentBlobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                               DateTimeOffset.UtcNow.AddDays(7));

            var blobSasBuilder = new BlobSasBuilder
            {
                BlobContainerName = this.blobContainer,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Write);

            StorageSharedKeyCredential storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, key.Value);

            string sas = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();

            return new StorageEntitySas
            {
                BlobUri = blobClient.Uri,
                Credentials = sas
            };

        }
        public struct StorageEntitySas
        {
            public string Credentials;
            public Uri BlobUri;
        }

    }
}

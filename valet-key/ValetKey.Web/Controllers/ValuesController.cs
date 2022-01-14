using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace WebApplication1.Controllers
{
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly string blobContainer;
        private IConfiguration configuration;


        public ValuesController(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.blobServiceClient = new BlobServiceClient(configuration.GetSection("AppSettings:StorageConnectionString").Value);
            this.blobContainer = configuration.GetSection("AppSettings:ContainerName").Value;
        }

        [HttpGet("Api/Values")]
        public string Get()
        {
            try
            {
                var blobName = Guid.NewGuid();

                // Retrieve a shared access signature of the location we should upload this file to
                var blobSas = this.GetSharedAccessReferenceForUpload(blobName.ToString());
                Trace.WriteLine(string.Format("Blob Uri: {0} - Shared Access Signature: {1}", blobSas.BlobUri, blobSas.Credentials));

                return JsonConvert.SerializeObject(blobSas);
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
        private StorageEntitySas GetSharedAccessReferenceForUpload(string blobName)
        {
            var blob = blobServiceClient.GetBlobContainerClient(this.blobContainer).GetBlobClient(blobName);

            var storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, this.configuration.GetSection("AppSettings:StorageKey").Value);

            var blobSasBuilder = new BlobSasBuilder

            {
                BlobContainerName = this.blobContainer,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Write);
            var sas = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();

            return new StorageEntitySas
            {
                BlobUri = blob.Uri,
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


// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Api.Controllers
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Azure.Storage.Sas;
    using Microsoft.Azure;

    public class ValuesController : ApiController
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly string blobContainer;

        public ValuesController()
        {
            this.blobServiceClient = new BlobServiceClient(CloudConfigurationManager.GetSetting("Storage"));
            this.blobContainer = ConfigurationManager.AppSettings["ContainerName"];
        }

        // GET api/Values
        public StorageEntitySas Get()
        {
            try
            {
                var blobName = Guid.NewGuid();

                // Retrieve a shared access signature of the location we should upload this file to
                var blobSas = this.GetSharedAccessReferenceForUpload(blobName.ToString());
                Trace.WriteLine(string.Format("Blob Uri: {0} - Shared Access Signature: {1}", blobSas.BlobUri, blobSas.Credentials));

                return blobSas;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
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

            //find AzureStorageEmulatorAccountKey in https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator.
            var storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, "<AzureStorageEmulatorAccountKey>");

            var blobSasBuilder = new BlobSasBuilder

            {
                BlobContainerName = this.blobContainer,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            };
            policy.SetPermissions(BlobSasPermissions.Write);
            var sas = policy.ToSasQueryParameters(storageSharedKeyCredential).ToString();
       
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


// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Api.Controllers
{
    using System;
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
            string a = CloudConfigurationManager.GetSetting("Storage");
            this.blobServiceClient = new BlobServiceClient(CloudConfigurationManager.GetSetting("Storage"));
            this.blobContainer = "valetkeysample";
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
            var container = blobServiceClient.GetBlobContainerClient(this.blobContainer);
         
            var blob = container.GetBlobClient(blobName);

            StorageSharedKeyCredential storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");

            //UriBuilder sasUri = new UriBuilder(blob.Uri);

            var policy = new BlobSasBuilder

            {
                Protocol = SasProtocol.HttpsAndHttp,
                BlobContainerName = this.blobContainer,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                IPRange = new SasIPRange(IPAddress.None, IPAddress.None)
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

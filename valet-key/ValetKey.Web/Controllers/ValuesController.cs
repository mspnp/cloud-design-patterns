// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Api.Controllers
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class ValuesController : ApiController
    {
        private readonly CloudStorageAccount account;
        private readonly string blobContainer;

        public ValuesController()
        {
            this.account = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Storage"));
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
            var blobClient = this.account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(this.blobContainer);

            var blob = container.GetBlockBlobReference(blobName);

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Write,

                // Create a signature for 5 min earlier to leave room for clock skew
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),

                // Create the signature for as long as necessary -  we can 
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(5)
            };

            var sas = blob.GetSharedAccessSignature(policy);

            return new StorageEntitySas
            {
                BlobUri = blob.Uri,
                Credentials = sas,
                Name = blobName
            };
        }

        public struct StorageEntitySas
        {
            public string Credentials;
            public Uri BlobUri;
            public string Name;
        }
    }
}

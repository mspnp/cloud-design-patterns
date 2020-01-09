// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Web.Controllers
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Azure.Storage.Sas;
    using Microsoft.Azure;


    public class HomeController : Controller
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly string blobContainer;

        public HomeController()
        {         
            this.blobServiceClient = new BlobServiceClient(CloudConfigurationManager.GetSetting("Storage"));
            this.blobContainer = ConfigurationManager.AppSettings["ContainerName"];
        }

        public async Task<ActionResult> RedirectTest(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentNullException("blobId", "Value cannot be null.  Correct usage: /Home/RedirectTest/{blob name}");
                }

                var blobSas = await this.GetSharedAccessReferenceForDownload(id);
                UriBuilder sasUri = new UriBuilder(blobSas.BlobUri);
                sasUri.Query = blobSas.Credentials;
                // Note that redirecting the user directly to the blob url may leak to IIS logs and/or browser history.
                return this.Redirect(sasUri.Uri.ToString());
            }
            catch (Exception ex)
            {
                var message = "Error: " + ex.Message;
                Trace.TraceError(message);
                ViewBag.ErrorMessage = message;

                return this.View("Error");
            }
        }

        public ActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// We return a limited access key that allows the caller to download a file to this specific destination for defined period of time
        /// </summary>
        private async Task<StorageEntitySas> GetSharedAccessReferenceForDownload(string blobName)
        {
            var blob = blobServiceClient.GetBlobContainerClient(this.blobContainer).GetBlobClient(blobName);

            var storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, ConfigurationManager.AppSettings["AzureStorageEmulatorAccountKey"]);

            var blobSasBuilder = new BlobSasBuilder

            {
                BlobContainerName = this.blobContainer,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
            };

            policy.SetPermissions(BlobSasPermissions.Read);
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

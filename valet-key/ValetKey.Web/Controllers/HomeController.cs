// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Web.Controllers
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class HomeController : Controller
    {
        private readonly CloudStorageAccount account;
        private readonly string blobContainer;

        public HomeController()
        {
            this.account = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Storage"));
            this.blobContainer = "valetkeysample";
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

                // Note that redirecting the user directly to the blob url may leak to IIS logs and/or browser history.
                return this.Redirect(string.Format("{0}{1}", blobSas.BlobUri, blobSas.Credentials));
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

        private async Task<StorageEntitySas> GetSharedAccessReferenceForDownload(string blobName)
        {
            var blobClient = this.account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(this.blobContainer);

            var blob = container.GetBlockBlobReference(blobName);
            
            if (!await blob.ExistsAsync())
            {
                throw new Exception("Blob does not exist");
            }

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,

                // Create a signature for 5 min earlier to leave room for clock skew
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),

                // Create the signature for as long as necessary -  we can 
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(5)
            };
            
            var sas = blob.GetSharedAccessSignature(policy);

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

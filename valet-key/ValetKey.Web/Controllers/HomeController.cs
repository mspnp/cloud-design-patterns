// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Web.Controllers
{
    using System;
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

        private async Task<StorageEntitySas> GetSharedAccessReferenceForDownload(string blobName)
        {
            var container = blobServiceClient.GetBlobContainerClient(this.blobContainer);

            var blob = container.GetBlobClient(blobName);

            StorageSharedKeyCredential storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName, "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");
       
            var policy = new BlobSasBuilder

            {
                Protocol = SasProtocol.None,
                BlobContainerName = this.blobContainer,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                IPRange = new SasIPRange(IPAddress.None, IPAddress.None)
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

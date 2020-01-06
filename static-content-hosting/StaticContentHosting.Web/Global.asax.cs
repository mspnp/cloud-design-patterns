// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace StaticContentHosting.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode,
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //Deploy static content when the application starts up
            //Note: This is here to setup the sample and would likely be part of the deployment process
            try
            {
                DeployStaticContent();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception Deploying Static Content - Message:{0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Deploy static content to the storage account setup for static content
        /// </summary>
        private void DeployStaticContent()
        {
            string connectionString = Settings.StaticContentStorageConnectionString;
            string containerName = Settings.StaticContentContainer;

            var account = new BlobServiceClient(connectionString);
            var staticContentContainer = account.GetBlobContainerClient(containerName);

            //Create the container with public access permissions on the blobs in those containers
            staticContentContainer.CreateIfNotExists(PublicAccessType.Blob);

            //Upload Images folder
            UploadFolder("Images", staticContentContainer);

            //Upload Scripts folder
            UploadFolder("Scripts", staticContentContainer);
        }

        /// <summary>
        /// Upload the files in the folder
        /// </summary>
        /// <param name="folderName">Folder in web application project to upload files from</param>
        /// <param name="container">Destination BLOB Storage container to upload files to</param>
        private void UploadFolder(string folderName, BlobContainerClient container)
        {
            Trace.TraceInformation("Uploading Static Content Folder - Container:{0} Folder:{1}", container, folderName);

            var imageFiles = Directory.GetFiles(Server.MapPath(folderName));
            foreach (var imageFile in imageFiles)
            {
                Trace.TraceInformation("Uploading File - Container:{0} Folder:{1} File:{2}", container, folderName, imageFile);

                var fileName = Path.GetFileName(imageFile);

                if (fileName == null)
                {
                    return;
                }

                var blobFile = container.GetBlobClient(string.Format("{0}/{1}", folderName, fileName));
                blobFile.Upload(imageFile);

                //We should check to see if the file has changed and update it
            }
        }
    }
}

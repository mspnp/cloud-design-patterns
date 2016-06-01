// ==============================================================================================================
// Microsoft patterns & practices
// Cloud Design Patterns project
// ==============================================================================================================
// ©2013 Microsoft. All rights reserved. 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================
namespace StaticContentHosting.Web
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

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
            var account = CloudStorageAccount.Parse(Settings.StaticContentStorageConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var staticContentContainer = blobClient.GetContainerReference(Settings.StaticContentContainer);

            //Create the container with public access permissions on the blobs in those containers
            staticContentContainer.CreateIfNotExists(BlobContainerPublicAccessType.Blob);

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
        private void UploadFolder(string folderName, CloudBlobContainer container)
        {
            Trace.TraceInformation("Uploading Static Content Folder - Container:{0} Folder:{1}", container, folderName);

            var imageFiles = Directory.GetFiles(Server.MapPath(folderName));
            foreach (var imageFile in imageFiles)
            {
                Trace.TraceInformation("Uploading File - Container:{0} Folder:{1} File:{2}", container, folderName, imageFile);

                var fileName = Path.GetFileName(imageFile);
                if (null != fileName)
                {
                    var blobFile = container.GetBlockBlobReference(string.Format("{0}/{1}", folderName, fileName));
                    if(!blobFile.Exists())
                        blobFile.UploadFromFile(imageFile, FileMode.Open);

                    //We should check to see if the file has changed and update it
                }
            }
        }
    }
}
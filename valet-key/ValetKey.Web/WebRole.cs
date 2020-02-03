// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Web
{
    using Azure.Storage.Blobs;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using System.Configuration;

    public class WebRole : RoleEntryPoint
    {
        private static readonly string BlobContainer = ConfigurationManager.AppSettings["ContainerName"];

        public override bool OnStart()
        {
            BlobContainerClient container = new BlobContainerClient(CloudConfigurationManager.GetSetting("Storage"), BlobContainer);
            container.CreateIfNotExists();

            return base.OnStart();
        }
    }
}

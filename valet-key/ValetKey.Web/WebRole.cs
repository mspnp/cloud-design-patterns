// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Web
{
    using Azure.Storage.Blobs;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WebRole : RoleEntryPoint
    {
        private const string BlobContainer = "valetkeysample";

        public override bool OnStart()
        {
            BlobContainerClient container = new BlobContainerClient(CloudConfigurationManager.GetSetting("Storage"), BlobContainer);
            container.CreateIfNotExists();

            return base.OnStart();
        }
    }
}

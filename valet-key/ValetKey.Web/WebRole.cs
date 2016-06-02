// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ValetKey.Web
{
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.WindowsAzure.Storage;

    public class WebRole : RoleEntryPoint
    {
        private const string BlobContainer = "valetkeysample";

        public override bool OnStart()
        {
            // Setup blob container
            var account = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Storage"));

            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(BlobContainer);
            container.CreateIfNotExists();

            return base.OnStart();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace HealthEndpointMonitoring.Web
{
    using Azure.Storage.Blobs;
    using Microsoft.Azure;


    public class Settings
    {
        public static BlobServiceClient StorageAccount
        {
            get
            {
                return new BlobServiceClient(CloudConfigurationManager.GetSetting("StorageAccount"));
            }
        }
    }
}
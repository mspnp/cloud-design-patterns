// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace HealthEndpointMonitoring.Web
{
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;

    public class Settings
    {
        public static CloudStorageAccount StorageAccount
        {
            get
            {
                return CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageAccount"));
            }
        }
    }
}
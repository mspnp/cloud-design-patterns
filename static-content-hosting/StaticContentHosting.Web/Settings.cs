// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace StaticContentHosting.Web
{
    public class Settings
    {
        public static string StaticContentStorageConnectionString
        {
            get
            {
                return RoleEnvironment.GetConfigurationSettingValue("StaticContent.StorageConnectionString");
            }
        }

        public static string StaticContentContainer
        {
            get
            {
                return RoleEnvironment.GetConfigurationSettingValue("StaticContent.Container");
            }
        }

        public static string StaticContentBaseUrl
        {
            get
            {
                var account = new BlobServiceClient(StaticContentStorageConnectionString);

                return string.Format("{0}/{1}", account.Uri.ToString().TrimEnd('/'), StaticContentContainer.TrimStart('/'));
            }
        }
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace StaticContentHosting.Web
{
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.WindowsAzure.Storage;

    public class Settings
    {
        public static string StaticContentStorageConnectionString {
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
                var account = CloudStorageAccount.Parse(StaticContentStorageConnectionString);

                return string.Format("{0}/{1}", account.BlobEndpoint.ToString().TrimEnd('/'), StaticContentContainer.TrimStart('/'));
            }
        }
    }
}
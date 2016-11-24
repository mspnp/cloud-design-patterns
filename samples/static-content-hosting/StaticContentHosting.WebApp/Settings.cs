// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace StaticContentHosting.WebApp
{
    using Microsoft.WindowsAzure.Storage;
    using System.Configuration;

    public class Settings
    {
        public static string StaticContentStorageConnectionString {
            get
            {
                return ConfigurationManager.AppSettings["StaticContent.StorageConnectionString"];
            }
        }

        public static string StaticContentContainer
        {
            get
            {
                return ConfigurationManager.AppSettings["StaticContent.Container"];
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
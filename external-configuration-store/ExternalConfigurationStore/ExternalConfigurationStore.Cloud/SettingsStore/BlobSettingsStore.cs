// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure;

namespace ExternalConfigurationStore.Cloud.SettingsStore
{
    public class BlobSettingsStore : ISettingsStore
    {
        private readonly BlobClient configBlob;

        public BlobSettingsStore(string environment) : this(CloudConfigurationManager.GetSetting("storageAccount"), "configuration", "configurationdata", environment)
        {
        }

        public BlobSettingsStore(string connectionString, string configContainer, string configBlobName, string environment)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var container = blobServiceClient.GetBlobContainerClient(configContainer);
            container.CreateIfNotExists();

            this.configBlob = container.GetBlobClient(configBlobName + "-" + environment + ".config");
        }

        public async Task<ETag> GetVersionAsync()
        {
            var response = await configBlob.GetPropertiesAsync();
            var eTag = response.Value.ETag;

            return eTag;
        }

        public async Task<Dictionary<string, string>> FindAllAsync()
        {
            return await this.ReadSettingsFromStorageAsync();
        }

        private async Task<Dictionary<string, string>> ReadSettingsFromStorageAsync()
        {
            XElement configFile;

            // Read the configuration blob and return the settings as a Dictionary.
            using (var stream = new MemoryStream())
            {
                await this.configBlob.DownloadToAsync(stream);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    configFile = XElement.Parse(reader.ReadToEnd());
                }
            }

            return configFile.Descendants("add").ToDictionary(x => x.Attribute("key").Value, x => x.Attribute("value").Value);
        }
    }
}

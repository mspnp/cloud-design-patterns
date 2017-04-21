// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ExternalConfigurationStore.Cloud.SettingsStore
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class BlobSettingsStore : ISettingsStore
    {
        private readonly CloudBlockBlob configBlob;

        public BlobSettingsStore(string environment) : this(CloudConfigurationManager.GetSetting("storageAccount"), "configuration", "configurationdata", environment)
        {
        }

        public BlobSettingsStore(string storageAccount, string configContainer, string configBlobName, string environment)
        {
            var account = CloudStorageAccount.Parse(storageAccount);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(configContainer);

            this.configBlob = container.GetBlockBlobReference(configBlobName + "-" + environment + ".config");
        }

        public async Task<string> GetVersionAsync()
        {
                await this.configBlob.FetchAttributesAsync();

                return this.configBlob.Properties.ETag;
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
                await this.configBlob.DownloadToStreamAsync(stream);

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

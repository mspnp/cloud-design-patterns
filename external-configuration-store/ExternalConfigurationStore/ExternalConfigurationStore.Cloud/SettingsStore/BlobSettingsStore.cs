// ==============================================================================================================
// Microsoft patterns & practices
// Cloud Design Patterns project
// ==============================================================================================================
// ©2013 Microsoft. All rights reserved. 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================
namespace ExternalConfigurationStore.Cloud.SettingsStore
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class BlobSettingsStore : ISettingsStore
    {
        private CloudBlockBlob configBlob;

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

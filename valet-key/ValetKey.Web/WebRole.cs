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
namespace ValetKey.Web
{
    using Microsoft.WindowsAzure;
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

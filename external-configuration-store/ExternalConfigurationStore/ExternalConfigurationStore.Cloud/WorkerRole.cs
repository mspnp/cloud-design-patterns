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
namespace ExternalConfigurationStore.Cloud
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.WindowsAzure.Storage;

    public class WorkerRole : RoleEntryPoint
    {
        // Storing blob data here for sample purposes only.
        private readonly string configContainer = "configuration";
        private readonly string configBlobNameProduction = "configurationdata-production.config";
        private readonly string configBlobNameStaging = "configurationdata-staging.config";

        private ManualResetEvent completeEvent = new ManualResetEvent(false);

        public override void Run()
        {
            // Start the monitoring configuration changes.
            ExternalConfiguration.Instance.StartMonitor();

            // Get a setting.
            var setting = ExternalConfiguration.Instance.GetAppSetting("setting1");
            Trace.TraceInformation("Worker Role: Get setting1, value: " + setting);

            this.completeEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Setup configuration blobs for production and staging.
            this.UploadConfigurationBlob();

            // Subscribe to event.
            ExternalConfiguration.Instance.Changed.Subscribe(
                m => Trace.TraceInformation("Configuration has changed.  Key:{0} Value:{1}", m.Key, m.Value),
                ex => Trace.TraceError("Error detected: " + ex.Message));

            return base.OnStart();
        }
        
        public override void OnStop()
        {
            ExternalConfiguration.Instance.StopMonitor();
            //this.configurationManager.Dispose();

            // Delete config blobs, cleanup. For sample purposes only.
            this.DeleteConfigurationBlob();

            // Exit Run loop.
            this.completeEvent.Set();

            base.OnStop();
        }

        /// <summary>
        /// Initialize the storage account with some sample configuration files
        /// </summary>
        private void UploadConfigurationBlob()
        {
            // Setup blobs for sample.
            var account = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("storageAccount"));
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(this.configContainer);
            container.CreateIfNotExists();

            var productionBlob = container.GetBlockBlobReference(this.configBlobNameProduction);
            productionBlob.UploadFromFile(this.configBlobNameProduction, System.IO.FileMode.Open);

            var stagingBlob = container.GetBlockBlobReference(this.configBlobNameStaging);
            stagingBlob.UploadFromFile(this.configBlobNameStaging, System.IO.FileMode.Open);
        }

        /// <summary>
        /// Clean up storage account, remove sample configuration files
        /// </summary>
        private void DeleteConfigurationBlob()
        {
            // Cleanup sample resources.
            var account = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("storageAccount"));
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(this.configContainer);

            container.DeleteIfExists();
        }
    }
}

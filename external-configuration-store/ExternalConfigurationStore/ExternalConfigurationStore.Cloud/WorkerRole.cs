// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.Azure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Azure.Storage.Blobs;

namespace ExternalConfigurationStore.Cloud
{
    public class WorkerRole : RoleEntryPoint
    {
        // Storing blob data here for sample purposes only.
        private const string configContainer = "configuration";
        private const string configBlobNameProduction = "configurationdata-production.config";
        private const string configBlobNameStaging = "configurationdata-staging.config";

        private readonly ManualResetEvent completeEvent = new ManualResetEvent(false);

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
            var connectionString = CloudConfigurationManager.GetSetting("storageAccount");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var container = blobServiceClient.GetBlobContainerClient(configContainer);
            container.CreateIfNotExists();

            var productionBlob = container.GetBlobClient(configBlobNameProduction);
            productionBlob.Upload(configBlobNameProduction);

            var stagingBlob = container.GetBlobClient(configBlobNameStaging);
            stagingBlob.Upload(configBlobNameStaging);
        }

        /// <summary>
        /// Clean up storage account, remove sample configuration files
        /// </summary>
        private void DeleteConfigurationBlob()
        {
            // Cleanup sample resources.
            var connectionString = CloudConfigurationManager.GetSetting("storageAccount");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var container = blobServiceClient.GetBlobContainerClient(configContainer);

            container.DeleteIfExists();
        }
    }
}

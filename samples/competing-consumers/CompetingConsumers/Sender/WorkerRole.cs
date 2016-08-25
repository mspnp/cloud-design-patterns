// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Sender
{
    using System.Net;
    using System.Threading;
    using CompetingConsumers.Shared;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WorkerRole : RoleEntryPoint
    {
        private QueueManager queueManager;
        private bool keepRunning = true;

        public override void Run()
        {
            while (this.keepRunning)
            {
                // Send messages in batch
                this.queueManager.SendMessagesAsync().Wait();

                Thread.Sleep(10000);
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            var queueName = CloudConfigurationManager.GetSetting("QueueName");
            var connectionString = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");

            this.queueManager = new QueueManager(queueName, connectionString);
            this.queueManager.Start().Wait();

            return base.OnStart();
        }

        public override void OnStop()
        {
            this.keepRunning = false;
            
            base.OnStop();
        }
    }
}

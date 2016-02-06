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
namespace Sender
{
    using System.Net;
    using System.Threading;
    using CompetingConsumers.Shared;
    using Microsoft.WindowsAzure;
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

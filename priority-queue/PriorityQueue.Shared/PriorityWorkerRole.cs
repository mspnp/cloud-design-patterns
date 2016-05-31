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
namespace PriorityQueue.Shared
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class PriorityWorkerRole : RoleEntryPoint
    {
        private QueueManager queueManager;
        private ManualResetEvent completedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            // Start listening for messages on the subscription.
            var subscriptionName = CloudConfigurationManager.GetSetting("SubscriptionName");
            this.queueManager.ReceiveMessages(subscriptionName, this.ProcessMessage);

            this.completedEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Make sure you provide the corresponding Service Bus connection information in the service configuration file.
            var connectionString = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
            var subscriptionName = CloudConfigurationManager.GetSetting("SubscriptionName");
            var topicName = CloudConfigurationManager.GetSetting("TopicName");

            this.queueManager = new QueueManager(connectionString, topicName);

            // create the subscriptions, one for each priority.
            this.queueManager.Setup(subscriptionName, priority: subscriptionName);

            return base.OnStart();
        }

        public override void OnStop()
        {
            this.queueManager.StopReceiver(TimeSpan.FromSeconds(30)).Wait();

            this.completedEvent.Set();

            base.OnStop();
        }

        protected virtual async Task ProcessMessage(BrokeredMessage message)
        {
            // simulating processing
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

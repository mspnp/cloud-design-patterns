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
namespace Receiver
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using CompetingConsumers.Shared;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WorkerRole : RoleEntryPoint
    {
        private QueueManager queueManager;
        private ManualResetEvent completedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            // Start listening for messages on the queue.
            this.queueManager.ReceiveMessages(this.ProcessMessage);

            this.completedEvent.WaitOne();
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
            this.queueManager.Stop(TimeSpan.FromSeconds(30)).Wait();
            this.completedEvent.Set();

            base.OnStop();
        }

        private async Task ProcessMessage(BrokeredMessage message)
        {
            try
            {
                if (!this.IsValidMessage(message))
                {
                    // Send the message to the Dead Letter queue for further analysis.
                    await message.DeadLetterAsync("Invalid message", "The message Id is invalid");
                    Trace.WriteLine("Invalid Message. Sending to Dead Letter queue");
                }

                // Simulate message processing.
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                Trace.WriteLine("Consumer " + RoleEnvironment.CurrentRoleInstance.Id + " : Message processed successfully: " + message.MessageId);

                // Complete the message.
                await message.CompleteAsync();
            }
            catch (Exception ex)
            {
                // Abandon the message when appropriate.  If the message reaches the MaxDeliveryCount limit, it will be automatically deadlettered.
                message.Abandon();
                Trace.TraceError("An error has occurred while processing the message: " + ex.Message);
            }
        }

        private bool IsValidMessage(BrokeredMessage message)
        {
            // Simulate message validation.
            return !string.IsNullOrWhiteSpace(message.MessageId);
        }
    }
}

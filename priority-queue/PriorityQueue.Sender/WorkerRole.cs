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
namespace PriorityQueue.Sender
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using PriorityQueue.Shared;

    public class WorkerRole : RoleEntryPoint
    {
        private QueueManager queueManager;
        private readonly ManualResetEvent stopRunningEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent exitingRunEvent = new ManualResetEvent(false);

        public override void Run()
        {
            //Test message send loop
            do
            {
                try
                {
                    // Send a low priority batch
                    var lowMessages = new List<BrokeredMessage>();

                    for (int i = 0; i < 10; i++)
                    {
                        var message = new BrokeredMessage() {MessageId = Guid.NewGuid().ToString()};
                        message.Properties["Priority"] = Priority.Low;
                        lowMessages.Add(message);
                    }

                    this.queueManager.SendBatchAsync(lowMessages).Wait();
                    Trace.TraceInformation("Sent low priority message batch: " + RoleEnvironment.CurrentRoleInstance.Id);

                    // Send a high priority batch
                    var highMessages = new List<BrokeredMessage>();

                    for (int i = 0; i < 10; i++)
                    {
                        var message = new BrokeredMessage() {MessageId = Guid.NewGuid().ToString()};
                        message.Properties["Priority"] = Priority.High;
                        highMessages.Add(message);
                    }

                    this.queueManager.SendBatchAsync(highMessages).Wait();
                    Trace.TraceInformation("Sent high priority message batch: " + RoleEnvironment.CurrentRoleInstance.Id);
                }
                catch (Exception ex)
                {
                    // We could check an exception count here and at some point choose to bubble this up for a role instance reset
                    //  If for example we have bad configuration or somethign that we cannot recover from we may raise it to the role instance
                    Trace.TraceError("Exception in initial sender: {0}", ex.Message);

                    // Avoid the situation where a configuration error or some other long term exception causes us to fill up the logs
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }

                // Continue processing while we are not signaled.
            }
            //Use this instead of sleeping so we can signal a stop while waiting
            while (!this.stopRunningEvent.WaitOne(TimeSpan.FromSeconds(30)));

            // Signal that we are exiting
            this.exitingRunEvent.Set();
        }

        public override bool OnStart()
        {
            // Make sure you provide the corresponding Service Bus connection information in the service configuration file.
            var serviceBusConnectionString = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
            var topicName = CloudConfigurationManager.GetSetting("TopicName");

            this.queueManager = new QueueManager(serviceBusConnectionString, topicName);
            this.queueManager.SetupTopic();

            return base.OnStart();
        }

        public override void OnStop()
        {
            // Signal the Run() loop to exit.
            this.stopRunningEvent.Set();

            // Stop the sender.
            this.queueManager.StopSender().Wait();

            // Wait for the Run() loop to complete it's current operation and exit
            this.exitingRunEvent.WaitOne(TimeSpan.FromMinutes(5));

            base.OnStop();
        }
    }
}

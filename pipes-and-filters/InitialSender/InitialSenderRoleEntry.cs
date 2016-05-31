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
namespace InitialSender
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Threading;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using PipesAndFilters.Shared;

    public class InitialSenderRoleEntry : RoleEntryPoint
    {
        private readonly ManualResetEvent exitingRunEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent stopRunningEvent = new ManualResetEvent(false);

        private QueueManager queueManager;

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            this.queueManager = new QueueManager(Constants.QueueAPath, Settings.ServiceBusConnectionString);
            this.queueManager.Start().Wait();

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            return base.OnStart();
        }

        public override void Run()
        {
            do
            {
                try
                {
                    // Create a new brokered message
                    var msg = new TestMessage()
                    {
                        Id = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
                        Text = "Sample Pipes and Filters Message"
                    };

                    // Create a brokered message, set the MessageId to the payloads Id
                    var brokeredMessage = new BrokeredMessage(msg)
                    {
                        MessageId = msg.Id
                    };

                    Trace.TraceInformation("New message sent:{0} at {1}", msg.Id, DateTime.UtcNow);

                    this.queueManager.SendMessageAsync(brokeredMessage).Wait();
                }
                catch (Exception ex)
                {
                    // We could check an exception count here and at some point choose to bubble this up for a role instance reset
                    Trace.TraceError("Exception in initial sender: {0}", ex.Message);

                    // Avoid the situation where a configuration error or some other long term exception causes us to fill up the logs
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }

                // Continue processing while we are not signaled.
            }
            while (!this.stopRunningEvent.WaitOne(TimeSpan.FromSeconds(60)));

            // Signal that we are exiting
            this.exitingRunEvent.Set();
        }

        public override void OnStop()
        {
            // Stop the queue and cleanup.
            this.queueManager.Stop().Wait();

            // Signal the Run() loop to exit.
            this.stopRunningEvent.Set();

            // Wait for the Run() loop to complete it's current operation and exit
            this.exitingRunEvent.WaitOne(TimeSpan.FromMinutes(5));
        }
    }
}

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
namespace FinalReceiver
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using PipesAndFilters.Shared;

    public class FinalReceiverRoleEntry : RoleEntryPoint
    {
        // Create and event un-signaled to block return from Run() event
        private readonly ManualResetEvent stopRunningEvent = new ManualResetEvent(false);

        // Final queue/pipe in our pipeline to process data from
        private ServiceBusPipeFilter queueFinal;

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Setup the queue.
            this.queueFinal = new ServiceBusPipeFilter(Settings.ServiceBusConnectionString, Constants.QueueFinalPath);
            this.queueFinal.Start();

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            return base.OnStart();
        }

        public override void Run()
        {
            this.queueFinal.OnPipeFilterMessageAsync(
                async (msg) =>
                {
                    await Task.Delay(500); // DOING WORK

                    // The pipeline message was received.
                    Trace.TraceInformation(
                        "Pipeline Message Complete - FilterA:{0} FilterB:{1}",
                        msg.Properties[Constants.FilterAMessageKey],
                        msg.Properties[Constants.FilterBMessageKey]);

                    return null;
                });

            // Wait for a stop event to move on
            this.stopRunningEvent.WaitOne();
        }

        public override void OnStop()
        {
            // Close the Queue message pump
            if (this.queueFinal != null)
            {
                this.queueFinal.Close(TimeSpan.FromSeconds(30)).Wait();
            }
            
            // Signal the Run() loop to exit.
            this.stopRunningEvent.Set();
        }
    }
}

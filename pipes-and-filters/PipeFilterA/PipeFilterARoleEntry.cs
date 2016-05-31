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
namespace PipeFilterA
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using PipesAndFilters.Shared;

    public class PipeFilterARoleEntry : RoleEntryPoint
    {
        private readonly ManualResetEvent stopRunningEvent = new ManualResetEvent(false);
        private ServiceBusPipeFilter pipeFilterA;

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            this.pipeFilterA = new ServiceBusPipeFilter(
               Settings.ServiceBusConnectionString,
               Constants.QueueAPath,
               Constants.QueueBPath);

            this.pipeFilterA.Start();

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            return base.OnStart();
        }

        public override void Run()
        {
            this.pipeFilterA.OnPipeFilterMessageAsync(async (msg) =>
            {
                // Clone the message and update it
                // Properties set by the broker (Deliver count, enqueue time, etc...) are not cloned and will need to be copied over if important.
                var newMsg = msg.Clone();

                // DOING WORK
                await Task.Delay(500); 

                Trace.TraceInformation("Fitler A processed message:{0} at {1}", msg.MessageId, DateTime.UtcNow);

                newMsg.Properties.Add(Constants.FilterAMessageKey, "Complete");

                return newMsg;
            });

            this.stopRunningEvent.WaitOne();
        }

        public override void OnStop()
        {
            // We will wait 10 seconds for our processing of the message to complete
            if (null != this.pipeFilterA)
            {
                this.pipeFilterA.Close(TimeSpan.FromSeconds(10)).Wait();
            }

            // Signal the Run() loop to exit.
            this.stopRunningEvent.Set();
        }
    }
}

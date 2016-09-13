// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipeFilterB
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using PipesAndFilters.Shared;

    public class PipeFilterBRoleEntry : RoleEntryPoint
    {
        private readonly ManualResetEvent stopRunningEvent = new ManualResetEvent(false);
        private ServiceBusPipeFilter pipeFilterB;

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            this.pipeFilterB = new ServiceBusPipeFilter(
               Settings.ServiceBusConnectionString,
               Constants.QueueBPath,
               Constants.QueueFinalPath);

            this.pipeFilterB.Start();

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            return base.OnStart();
        }

        public override void Run()
        {
            this.pipeFilterB.OnPipeFilterMessageAsync(async (msg) =>
            {
                // Clone the message and update it
                // Properties set by the broker (Deliver count, enqueue time, etc...) are not cloned and will need to be copied over if important.
                var newMsg = msg.Clone();

                // DOING SOME FILTER B WORK
                await Task.Delay(500);

                Trace.TraceInformation("Filter B processed message:{0} at {1}", msg.MessageId, DateTime.UtcNow);

                newMsg.Properties.Add(Constants.FilterBMessageKey, "Complete");

                return newMsg;
            });

            this.stopRunningEvent.WaitOne();
        }

        public override void OnStop()
        {
            // We will wait 10 seconds for our processing of the message to complete
            if (this.pipeFilterB != null)
            {
                this.pipeFilterB.Close(TimeSpan.FromSeconds(10)).Wait();
            }

            // Signal the Run() loop to exit.
            this.stopRunningEvent.Set();
        }
    }
}

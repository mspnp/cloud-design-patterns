// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

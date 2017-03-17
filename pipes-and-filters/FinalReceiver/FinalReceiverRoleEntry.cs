// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

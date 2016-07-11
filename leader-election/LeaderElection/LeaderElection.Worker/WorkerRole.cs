// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace LeaderElection.Worker
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using DistributedMutex;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Microsoft.WindowsAzure.Storage;

    public class WorkerRole : RoleEntryPoint
    {
        private CancellationTokenSource cts;
        private readonly ManualResetEvent runCompletedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            var settings = new BlobSettings(CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Storage")), "leases", "MyLeaderCoordinatorTask");

            var mutex = new BlobDistributedMutex(settings, MyLeaderCoordinatorTask);

            mutex.RunTaskWhenMutexAcquired(this.cts.Token).Wait();

            Trace.TraceInformation("Returning from WorkerRole Run and signaling runCompletedEvent");
            this.runCompletedEvent.Set();
        }

        public override bool OnStart()
        {
            this.cts = new CancellationTokenSource();

            return base.OnStart();
        }

        public override void OnStop()
        {
            //Cancel the leader task
            this.cts.Cancel();

            //Wait for run to complete and exit before returning from onstop
            this.runCompletedEvent.WaitOne(TimeSpan.FromMinutes(5));

            base.OnStop();
        }

        private static async Task MyLeaderCoordinatorTask(CancellationToken token)
        {
            // Fixed interval to wake up check for work and/or do work
            var interval = TimeSpan.FromSeconds(10);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Wake up and do some background processing if not canceled
                    // PROCESSING CODE
                    Trace.TraceInformation("Doing Leader Task Work");

                    // Back to sleep for a period of time unless we are asked to cancel
                    await Task.Delay(interval, token);
                }
            }
            catch (OperationCanceledException)
            {
                // expect this exception to be thrown in normal circumstances or check the cancellation token, because
                // if the lease can't be renewed, the token will signal a cancellation request.
                Trace.TraceInformation("Aborting work, as the lease has been lost");
            }
        }
    }
}

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
namespace LeaderElection.Worker
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using DistributedMutex;
    using Microsoft.WindowsAzure;
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

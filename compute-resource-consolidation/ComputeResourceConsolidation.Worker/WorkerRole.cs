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
namespace ComputeResourceConsolidation.Worker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WorkerRole : RoleEntryPoint
    {
        /// <summary>
        /// The cancellation token source use to cooperatively cancel running tasks
        /// </summary>
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// List of running tasks on the role instance
        /// </summary>
        private readonly List<Task> tasks = new List<Task>();

        /// <summary>
        /// List of worker tasks to run on this role
        /// </summary>
        private readonly List<Func<CancellationToken, Task>> workerTasks = new List<Func<CancellationToken, Task>>
        {
            MyWorkerTask1,
            MyWorkerTask2
        };

        /// <summary>
        /// RoleEntry Run() is called after OnStart().  Returning from run will cause a Role instance to recycle.
        /// </summary>
        public override void Run()
        {
            // Start worker tasks and add to the task list
            foreach (var worker in this.workerTasks)
            {
                this.tasks.Add(worker(this.cts.Token));
            }

            Trace.TraceInformation("Worker host tasks started");

            // The assumption is that all tasks should remain running and not return, similar to role entry Run() behavior.
            try
            {
                Task.WaitAny(this.tasks.ToArray());
            }
            catch (AggregateException ex)
            {
                Trace.TraceError(ex.Message);

                // If any of the inner exceptions in the aggregate exception are not cancellation exceptions then rethrow the exception
                ex.Handle(innerEx => (innerEx is OperationCanceledException));
            }

            // If there was not a cancellation request, stop all tasks and return from run
            // Another option to canceling and returning when a task exits would be to restart the task
            if (!this.cts.IsCancellationRequested)
            {
                Trace.TraceInformation("Task returned without cancellation request");
                this.Stop(TimeSpan.FromMinutes(5));
            }
        }

        /// <summary>
        /// Role Entry OnStart is called before Run() and the Azure fabric does not consider a role online and is not added to the load balancer until this method returns
        /// Perform initialization work here that is necessary to begin accepting requests
        /// </summary>
        /// <returns></returns>
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            return base.OnStart();
        }

        /// <summary>
        /// Role Entry OnStop() Raised when a role instance is stopping.
        /// </summary>
        public override void OnStop()
        {
            Trace.TraceInformation("OnStop called.  Canceling tasks.");

            this.Stop(TimeSpan.FromMinutes(5));

            Trace.TraceInformation("Tasks completed calling base.OnStop()");

            base.OnStop();
        }

        /// <summary>
        /// A sample worker role task #1
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task MyWorkerTask1(CancellationToken ct)
        {
            // Fixed interval to wake up check for work and/or do work
            var interval = TimeSpan.FromSeconds(30);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Wake up and do some background processing if not canceled
                    // PROCESSING CODE
                    Trace.TraceInformation("Doing Worker Task 1 Work");

                    // Back to sleep for a period of time unless we are asked to cancel
                    // Task.Delay will throw an OperationCancellationException when cancelled
                    await Task.Delay(interval, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Expect this exception to be thrown in normal circumstances or check the cancellation token, because
                // if the role instances are shutting down a cancellation request will be signalled.
                Trace.TraceInformation("Stopping service, cancellation requested");

                // Rethrow the exception
                throw;
            }
        }

        /// <summary>
        /// A sample worker role task #2
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task MyWorkerTask2(CancellationToken ct)
        {
            // Fixed interval to wake up check for work and/or do work
            var interval = TimeSpan.FromMinutes(5);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Wake up and do some background processing if not canceled
                    // PROCESSING CODE
                    Trace.TraceInformation("Doing Worker Task 2 Work");

                    // Back to sleep for a period of time unless we are asked to cancel
                    // Task.Delay will throw an OperationCancellationException when cancelled
                    await Task.Delay(interval, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Expect this exception to be thrown in normal circumstances or check the cancellation token, because
                // if the role instances are shutting down a cancellation request will be signalled.
                Trace.TraceInformation("Stopping service, cancellation requested");

                // Rethrow the exception
                throw;
            }
        }

        /// <summary>
        /// Stop running tasks and wait for tasks to complete before returning unless timeout expires
        /// </summary>
        /// <param name="timeout"></param>
        private void Stop(TimeSpan timeout)
        {
            Trace.TraceInformation("Stop called.  Canceling tasks.");

            // Cancel running tasks
            this.cts.Cancel();

            Trace.TraceInformation("Waiting for canceled tasks to finish and return");

            // Wait for all the tasks to complete before returning
            // Note that the emulator currently gives us 30 seconds and Azure 5min to complete processing
            try
            {
                Task.WaitAll(this.tasks.ToArray(), timeout);
            }
            catch (AggregateException ex)
            {
                Trace.TraceError(ex.Message);

                // If any of the inner exceptions in the aggregate exception are not cancellation exceptions then rethrow the exception
                ex.Handle(innerEx => (innerEx is OperationCanceledException));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Diagnostics;
using DistributedMutex;
using Microsoft.WindowsAzure.Storage;
using System.Fabric.Description;
using System.Collections.ObjectModel;

namespace LeaderElectionFabric.Worker
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Worker : StatelessService
    {
        public Worker(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ConfigurationPackage configPackage = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            // Access Settings.xml
            KeyedCollection<string, ConfigurationProperty> parameters = configPackage.Settings.Sections["AzureConfigSection"].Parameters;

            var storageConfig = parameters["Storage"]?.Value;

            var settings = new BlobSettings(
                CloudStorageAccount.Parse(storageConfig),
                "leases",
                "MyLeaderCoordinatorTask");

            var mutex = new BlobDistributedMutex(
                settings,
                MyLeaderCoordinatorTask);

            await mutex.RunTaskWhenMutexAcquired(cancellationToken);

            Trace.TraceInformation("Returning from WorkerRole Run and signaling runCompletedEvent");
        }

        private async Task MyLeaderCoordinatorTask(CancellationToken token)
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

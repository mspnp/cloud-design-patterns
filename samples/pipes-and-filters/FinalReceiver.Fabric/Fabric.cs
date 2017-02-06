using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Net;
using PipesAndFilters.Shared;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Fabric.Description;

namespace FinalReceiver.Fabric
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Fabric : StatelessService
    {
        // Final queue/pipe in our pipeline to process data from
        private ServiceBusPipeFilter queueFinal;

        public Fabric(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Get ServiceBus ConnectionString from configuration (Settings.xml)
            ConfigurationPackage configPackage = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            // Access Settings.xml
            KeyedCollection<string, ConfigurationProperty> parameters = configPackage.Settings.Sections["ConfigSection"].Parameters;

            string connectionString = parameters["ServiceBus.ConnectionString"]?.Value;

            // Setup the queue.
            this.queueFinal = new ServiceBusPipeFilter(connectionString, Constants.QueueFinalPath);
            this.queueFinal.Start();

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

            cancellationToken.WaitHandle.WaitOne();
        }
    }
}

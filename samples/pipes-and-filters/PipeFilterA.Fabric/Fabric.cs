using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using PipesAndFilters.Shared;
using System.Net;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Fabric.Description;

namespace PipeFilterA.Fabric
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Fabric : StatelessService
    {
        private ServiceBusPipeFilter pipeFilterA;

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

            this.pipeFilterA = new ServiceBusPipeFilter(
               connectionString,
               Constants.QueueAPath,
               Constants.QueueBPath);

            this.pipeFilterA.Start();

            this.pipeFilterA.OnPipeFilterMessageAsync(async (msg) =>
            {
                // Clone the message and update it
                // Properties set by the broker (Deliver count, enqueue time, etc...) are not cloned and will need to be copied over if important.
                var newMsg = msg.Clone();

                // DOING WORK
                await Task.Delay(500);

                Trace.TraceInformation("Filter A processed message:{0} at {1}", msg.MessageId, DateTime.UtcNow);

                newMsg.Properties.Add(Constants.FilterAMessageKey, "Complete");

                return newMsg;
            });

            cancellationToken.WaitHandle.WaitOne();

            // We will wait 10 seconds for our processing of the message to complete
            if (null != this.pipeFilterA)
            {
                await this.pipeFilterA.Close(TimeSpan.FromSeconds(10));
            }
        }
    }
}

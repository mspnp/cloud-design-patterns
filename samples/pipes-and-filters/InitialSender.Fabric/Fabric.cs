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
using Microsoft.ServiceBus.Messaging;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Fabric.Description;

namespace InitialSender.Fabric
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Fabric : StatelessService
    {
        private QueueManager queueManager;

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

            this.queueManager = new QueueManager(Constants.QueueAPath, connectionString);
            await this.queueManager.Start();

            do
            {
                try
                {
                    // Create a new brokered message
                    var msg = new TestMessage()
                    {
                        Id = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
                        Text = "Sample Pipes and Filters Message"
                    };

                    // Create a brokered message, set the MessageId to the payloads Id
                    var brokeredMessage = new BrokeredMessage(msg)
                    {
                        MessageId = msg.Id
                    };

                    Trace.TraceInformation("New message sent:{0} at {1}", msg.Id, DateTime.UtcNow);

                    this.queueManager.SendMessageAsync(brokeredMessage).Wait();
                }
                catch (Exception ex)
                {
                    // We could check an exception count here and at some point choose to bubble this up for a role instance reset
                    Trace.TraceError("Exception in initial sender: {0}", ex.Message);

                    // Avoid the situation where a configuration error or some other long term exception causes us to fill up the logs
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }

                // Continue processing while we are not signaled.
                await Task.Delay(TimeSpan.FromSeconds(60));
            }
            while (!cancellationToken.IsCancellationRequested);

            // Stop the queue and cleanup.
            await this.queueManager.Stop();

            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(5));
        }
    }
}

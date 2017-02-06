using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using PriorityQueue.Shared;
using System.Collections.ObjectModel;
using System.Fabric.Description;
using Microsoft.ServiceBus.Messaging;
using System.Diagnostics;

namespace PriorityQueue.Sender.Fabric
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

            // Get ServiceBus ConnectionString from configuration (Settings.xml)
            ConfigurationPackage configPackage = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            // Access Settings.xml
            KeyedCollection<string, ConfigurationProperty> parameters = configPackage.Settings.Sections["ConfigSection"].Parameters;

            var serviceBusConnectionString = parameters["ServiceBus.ConnectionString"]?.Value;
            var topicName = parameters["TopicName"]?.Value;

            this.queueManager = new QueueManager(serviceBusConnectionString, topicName);
            this.queueManager.SetupTopic();

            //Test message send loop
            do
            {
                try
                {
                    // Send a low priority batch
                    var lowMessages = new List<BrokeredMessage>();

                    for (int i = 0; i < 10; i++)
                    {
                        var message = new BrokeredMessage() { MessageId = Guid.NewGuid().ToString() };
                        message.Properties["Priority"] = Priority.Low;
                        lowMessages.Add(message);
                    }

                    await this.queueManager.SendBatchAsync(lowMessages);
                    Trace.TraceInformation("Sent low priority message batch: " + this.Context.NodeContext.NodeId.ToString());

                    // Send a high priority batch
                    var highMessages = new List<BrokeredMessage>();

                    for (int i = 0; i < 10; i++)
                    {
                        var message = new BrokeredMessage() { MessageId = Guid.NewGuid().ToString() };
                        message.Properties["Priority"] = Priority.High;
                        highMessages.Add(message);
                    }

                    await this.queueManager.SendBatchAsync(highMessages);
                    Trace.TraceInformation("Sent high priority message batch: " + this.Context.NodeContext.NodeId.ToString());
                }
                catch (Exception ex)
                {
                    // We could check an exception count here and at some point choose to bubble this up for a role instance reset
                    //  If for example we have bad configuration or somethign that we cannot recover from we may raise it to the role instance
                    Trace.TraceError("Exception in initial sender: {0}", ex.Message);

                    // Avoid the situation where a configuration error or some other long term exception causes us to fill up the logs
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }

                // Continue processing while we are not signaled.
            }

            //Use this instead of sleeping so we can signal a stop while waiting
            while (!cancellationToken.IsCancellationRequested);

            // Stop the sender.
            await this.queueManager.StopSender();

            // Wait for the Run() loop to complete it's current operation and exit
            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(5));
        }
    }
}

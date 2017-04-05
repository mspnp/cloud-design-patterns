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
using System.Linq;

namespace PriorityQueue.Sender
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
            await this.queueManager.SetupTopicAsync()
                .ConfigureAwait(false);

            //Test message send loop
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Send a low priority batch
                    var lowMessages = Enumerable.Range(0, 10)
                        .Select(i =>
                        {
                            var message = new BrokeredMessage()
                            {
                                MessageId = Guid.NewGuid().ToString()
                            };
                            message.Properties["Priority"] = Priority.Low;
                            return message;
                        }).ToList();
                    await this.queueManager.SendBatchAsync(lowMessages)
                        .ConfigureAwait(false);
                    Trace.TraceInformation($"Sent low priority message batch: {this.Context.NodeContext.NodeId.ToString()}");

                    // Send a high priority batch
                    var highMessages = Enumerable.Range(0, 10)
                        .Select(i =>
                        {
                            var message = new BrokeredMessage()
                            {
                                MessageId = Guid.NewGuid().ToString()
                            };
                            message.Properties["Priority"] = Priority.High;
                            return message;
                        }).ToList();

                    await this.queueManager.SendBatchAsync(highMessages)
                        .ConfigureAwait(false);
                    Trace.TraceInformation($"Sent high priority message batch: {this.Context.NodeContext.NodeId.ToString()}");
                }
                catch (Exception ex)
                {
                    // We could check an exception count here and at some point choose to bubble this up for a role instance reset
                    //  If for example we have bad configuration or somethign that we cannot recover from we may raise it to the role instance
                    Trace.TraceError($"Exception in initial sender: {ex.Message}");

                    // Avoid the situation where a configuration error or some other long term exception causes us to fill up the logs
                    await Task.Delay(TimeSpan.FromSeconds(30))
                        .ConfigureAwait(false);
                }
            }

            // Wait for the Run() loop to complete it's current operation and exit
            if (cancellationToken.WaitHandle.WaitOne())
            {
                // Stop the sender.
                await this.queueManager.StopSenderAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}

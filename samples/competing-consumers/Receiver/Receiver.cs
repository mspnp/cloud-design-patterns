using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompetingConsumers.Shared;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Net;

namespace Receiver
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Receiver : StatelessService
    {
        private QueueManager queueManager;
        
        public Receiver(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() => new ServiceInstanceListener[0];

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            var configurationPackage = Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            var queueName = configurationPackage.Settings.Sections["SettingsSection"].Parameters["QueueName"].Value;
            var connectionString = configurationPackage.Settings.Sections["SettingsSection"].Parameters["ServiceBusConnectionString"].Value;

            this.queueManager = new QueueManager(queueName, connectionString);
            await this.queueManager.StartAsync()
                .ConfigureAwait(false);
            
            // Start listening for messages on the queue.
            this.queueManager.ReceiveMessages(this.ProcessMessageAsync, cancellationToken);

            // Block until we are cancelled.
            if (cancellationToken.WaitHandle.WaitOne())
            {
                await this.queueManager.StopAsync()
                    .ConfigureAwait(false);
            }
        }

        private async Task ProcessMessageAsync(BrokeredMessage message)
        {
            try
            {
                if (!IsValidMessage(message))
                {
                    // Send the message to the Dead Letter queue for further analysis.
                    await message.DeadLetterAsync("Invalid message", "The message Id is invalid")
                        .ConfigureAwait(false);
                    Trace.WriteLine("Invalid Message. Sending to Dead Letter queue");
                }

                // Simulate message processing.
                await Task.Delay(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(false);

                Trace.WriteLine($"Consumer {this.Context.NodeContext.NodeId.ToString()} : Message processed successfully: {message.MessageId}");
                
                // Complete the message.
                await message.CompleteAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Abandon the message when appropriate.  If the message reaches the MaxDeliveryCount limit, it will be automatically deadlettered.
                await message.AbandonAsync()
                    .ConfigureAwait(false);
                Trace.TraceError($"An error has occurred while processing the message: {ex.Message}");
            }
        }

        private static bool IsValidMessage(BrokeredMessage message) => !string.IsNullOrWhiteSpace(message.MessageId);
    }
}

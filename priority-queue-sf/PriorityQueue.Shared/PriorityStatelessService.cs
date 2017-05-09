using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PriorityQueue.Shared
{
    public class PriorityStatelessService : StatelessService
    {
        private QueueManager queueManager;

        public PriorityStatelessService(StatelessServiceContext context)
            : base(context)
        { }

        protected virtual async Task ProcessMessageAsync(BrokeredMessage message) =>
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

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

            // Make sure you provide the corresponding Service Bus connection information in the service configuration file.
            var serviceBusConnectionString = parameters["ServiceBus.ConnectionString"]?.Value;
            var topicName = parameters["TopicName"]?.Value;
            var subscriptionName = parameters["SubscriptionName"]?.Value;

            this.queueManager = new QueueManager(serviceBusConnectionString, topicName);

            // create the subscriptions, one for each priority.
            await this.queueManager.SetupAsync(subscriptionName, priority: subscriptionName)
                .ConfigureAwait(false);

            this.queueManager.ReceiveMessages(subscriptionName, this.ProcessMessageAsync, cancellationToken);

            if (cancellationToken.WaitHandle.WaitOne())
            {
                await this.queueManager.StopReceiverAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}

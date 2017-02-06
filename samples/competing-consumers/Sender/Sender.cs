using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using CompetingConsumers.Shared;

namespace Sender
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Sender : StatelessService
    {
        private QueueManager queueManager;

        public Sender(StatelessServiceContext context)
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
            var configurationPackage = Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            var queueName = configurationPackage.Settings.Sections["SettingsSection"].Parameters["QueueName"].Value;
            var connectionString = configurationPackage.Settings.Sections["SettingsSection"].Parameters["ServiceBusConnectionString"].Value;

            this.queueManager = new QueueManager(queueName, connectionString);
            await this.queueManager.StartAsync()
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                await this.queueManager.SendMessagesAsync()
                    .ConfigureAwait(false);

                // Do not pass the cancellation token as it will throw an OperationCanceledException.
                await Task.Delay(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);
            }

            await this.queueManager.StopAsync()
                    .ConfigureAwait(false);
        }
    }
}

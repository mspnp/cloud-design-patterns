using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace ConfigurationService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ConfigurationService : StatefulService, IConfigurationService
    {
        private IReliableDictionary<string, ApplicationSettings> configurationRepo = null;
        public ConfigurationService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<ApplicationSettings> GetConfiguration(string enviroment)
        {
            ConditionalValue<ApplicationSettings> result;
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    result = await configurationRepo.TryGetValueAsync(tx, enviroment.ToLower());
                }

                return result.Value;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(
                this.Context,
                "Error while reading value for environment: {0}. Message: {1}. Rethrowing",
                enviroment,
                ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(context =>
                this.CreateServiceRemotingListener(context)) };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            configurationRepo = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ApplicationSettings>>("configurationRepo");
            ServiceEventSource.Current.ServiceMessage(
                this.Context,
                "Configuration Repository re/born from the ashes at Node: {0}",
                this.Context.NodeContext.NodeName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await Initialization(tx);
                await tx.CommitAsync();
            }
        }

        private async Task Initialization(ITransaction tx)
        {
            new List<ConfigurationSchema> {
                new ConfigurationSchema {
                    Environment = "development",
                    Settings = new ApplicationSettings {
                        Setting1 = "dev-setting1",
                        Setting2 = "dev-setting2"
                    }
                },
                new ConfigurationSchema
                {
                    Environment = "testing",
                    Settings = new ApplicationSettings
                    {
                        Setting1 = "test-setting1",
                        Setting2 = "test-setting2"
                    }
                },
                new ConfigurationSchema
                {
                    Environment = "production",
                    Settings = new ApplicationSettings
                    {
                        Setting1 = "prod-setting1",
                        Setting2 = "prod-setting2"
                    }
                }
            }.ForEach(cs => {
                this.configurationRepo.AddOrUpdateAsync(
                    tx, 
                    cs.Environment, 
                    cs.Settings,
                    (k, v) =>
                    {
                        return cs.Settings;
                    });
            });

            ServiceEventSource.Current.ServiceMessage(
                this.Context,
                "Configuration Repository has been filled/updated from origin. Keys: {0}",
                await this.configurationRepo.GetCountAsync(tx));
        }
    }
}

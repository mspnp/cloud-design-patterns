using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric.Description;
using System.Collections.ObjectModel;

namespace RuntimeReconfigurationFabric.Stateful
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Stateful : StatefulService
    {
        private string customSetting;

        public Stateful(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            ConfigurationPackage configPackage = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            this.UpdateConfigurationValues(configPackage.Settings, "Initilisation");

            this.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent +=
                                    this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;

            this.Context.CodePackageActivationContext.CodePackageModifiedEvent +=
                                    this.CodePackageActivationContext_CodePackageModifiedEvent;

            this.Context.CodePackageActivationContext.DataPackageModifiedEvent +=
                                    this.CodePackageActivationContext_DataPackageModifiedEvent;

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0} - Setting: {1}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.", customSetting);

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private void UpdateConfigurationValues(ConfigurationSettings settings, string origin)
        {
            try
            {
                KeyedCollection<string, ConfigurationProperty> parameters = settings.Sections["RuntimeReconfigurationConfigSection"].Parameters;

                customSetting = parameters["CustomSetting"]?.Value;

                ServiceEventSource.Current.ServiceMessage(this.Context, "Origin: {0}. CustomSetting value: {1}", origin, customSetting);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(ex.Message);
                throw;
            }
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "Configuration Package Changed notification.Settings were changed: {0}", e.NewPackage.Description);
            this.UpdateConfigurationValues(e.NewPackage.Settings, "Reconfiguration Event");
        }

        private void CodePackageActivationContext_CodePackageModifiedEvent(object sender, PackageModifiedEventArgs<CodePackage> e)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "Code Package Changed notification.Settings changed: {0}", e.NewPackage.Description);
        }

        private void CodePackageActivationContext_DataPackageModifiedEvent(object sender, PackageModifiedEventArgs<DataPackage> e)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "Data Package Changed notification.Settings changed: {0}", e.NewPackage.Description);
        }
    }
}

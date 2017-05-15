using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using LeaderStatefulService.Store;
using Shared;
using Shared.Service;

namespace LeaderStatefulService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    public sealed class LeaderStatefulService : StatefulService, ILeaderService
    {
        public const string ApplicationLogWorkloadName = "applicationlog-workload";
        private readonly bool simulateInternalFailure = bool.Parse(ConfigurationManager.AppSettings["simulateInternalFailure"]);

        WorkloadManager manager;
        IReliableDictionary<string, WorkloadManager> workloads;

        public LeaderStatefulService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<List<ApplicationLog>> GetWorkloadChunk()
        {
            await InitWorkloads();

            await DoInTransaction(instance =>
            {
                instance.Page = instance.Page + 1;
                return instance;
            });

            var result = await manager.GetNextChunk();

            if (result == null || result.Count == 0)
            {
                await DoInTransaction(instance =>
                {
                    instance.AggregatedTotal = 0;
                    instance.Page = 0;
                    return instance;
                });

                result = await manager.GetNextChunk();
            }

            return result;
        }

        public async Task ReportResult(int total)
        {
            await DoInTransaction(instance =>
            {
                instance.AggregatedTotal = instance.AggregatedTotal + total;
                return instance;
            });

            ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Manager fresh data. Leader at {0}. Aggregated: {1}, Page: {2}",
                        Context.NodeContext.NodeName,
                        manager.AggregatedTotal,
                        manager.Page);
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
            await InitWorkloads();
            
            using (var tx = this.StateManager.CreateTransaction())
            {
                try
                {
                    manager = await workloads.GetOrAddAsync(tx, ApplicationLogWorkloadName, new WorkloadManager());

                    await tx.CommitAsync();

                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Manager as the Fenix, under newly Elected Leader at {0}. Aggregated: {1}, Page: {2}",
                        Context.NodeContext.NodeName,
                        manager.AggregatedTotal,
                        manager.Page);
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Failure at RunAsync on Leader {0}. {1}",
                        Context.NodeContext.NodeName,
                        ex.Message);
                    throw;
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(20000, cancellationToken);

                // Simulate an exception raised in the leader
                if (simulateInternalFailure && manager.Page > 0 && (manager.Page % 3) == 0)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "Leader {0} forced takedown...", Context.NodeContext.NodeName);
                    throw new ApplicationException("Force failure");
                }
            }
        }

        private async Task InitWorkloads()
        {
            if (workloads != null)
                return;

            workloads = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, WorkloadManager>>("workloads");
        }

        private async Task DoInTransaction(Func<WorkloadManager, WorkloadManager> func)
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                await workloads.AddOrUpdateAsync(tx, ApplicationLogWorkloadName, manager, (key, instance) => func(instance));

                await tx.CommitAsync();
            }
        }

    }
}
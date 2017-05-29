using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Shared.Service;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System.Configuration;
using System.Fabric.Query;
using System.Linq;
using StatelessService = Microsoft.ServiceFabric.Services.Runtime.StatelessService;

namespace WorkerStatelessService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class WorkerStatelessService : StatelessService
    {
        private static readonly string LeaderServiceName = ConfigurationManager.AppSettings["LeaderServiceName"];
        private const int SecondsBetweenRetries = 5;
        private const int NumberOfRetries = 30;
        private const int Timeout = 60;

        public WorkerStatelessService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var serviceUri = new Uri("fabric:" + LeaderServiceName);
            await WaitForLeaderAsync(LeaderServiceName, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var service = ServiceProxy.Create<ILeaderService>(serviceUri, new ServicePartitionKey(1));

                var load = await service.GetWorkloadChunkAsync();

                var total = 0;
                load.ForEach(e => total = total + e.Total);

                await service.ReportResultAsync(total);

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }

        private static async Task WaitForLeaderAsync(string leaderServiceName, CancellationToken cancellationToken)
        {
            var count = 0;
            while (count < NumberOfRetries && !cancellationToken.IsCancellationRequested)
            {
                var fabricClient = new FabricClient();
                var apps = fabricClient.QueryManager.GetApplicationListAsync(null, TimeSpan.FromSeconds(Timeout), cancellationToken).Result;

                if (apps.Any())
                {
                    var services = await fabricClient.QueryManager.GetServiceListAsync(apps.First().ApplicationName, null, null,
                                TimeSpan.FromSeconds(Timeout), cancellationToken);
                    var leaderService = services.FirstOrDefault(s => s.ServiceName.LocalPath == leaderServiceName);
                    if (leaderService != null && leaderService.ServiceStatus == ServiceStatus.Active)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(SecondsBetweenRetries), cancellationToken);
                        return;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(SecondsBetweenRetries), cancellationToken);
                count++;
            }
            throw new TimeoutException("Leader not initialized after " + count * SecondsBetweenRetries + " seconds" );
        }

    }
}

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
        private static readonly int secondsBetweenRetries = 5;
        private static readonly int numberOfRetries = 30;
        private static readonly int timeout = 60;

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
            await WaitForLeader(LeaderServiceName, cancellationToken);

            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            while (!cancellationToken.IsCancellationRequested)
            {
                var service = ServiceProxy.Create<ILeaderService>(serviceUri, new ServicePartitionKey(1));

                var load = await service.GetWorkloadChunk();

                var total = 0;
                load.ForEach(e => total = total + e.Total);

                await service.ReportResult(total);

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }

        private static async Task WaitForLeader(string leaderServiceName, CancellationToken cancellationToken, int count = 1)
        {
            while (count < numberOfRetries && !cancellationToken.IsCancellationRequested)
            {
                var fabricClient = new FabricClient();
                var apps = fabricClient.QueryManager.GetApplicationListAsync(null, TimeSpan.FromSeconds(timeout), cancellationToken).Result;

                if (apps.Any())
                {
                    var services = await fabricClient.QueryManager.GetServiceListAsync(apps.First().ApplicationName, null, null,
                                TimeSpan.FromSeconds(timeout), cancellationToken);
                    var leaderService = services.FirstOrDefault(s => s.ServiceName.LocalPath == leaderServiceName);
                    if (leaderService != null && leaderService.ServiceStatus == ServiceStatus.Active)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(secondsBetweenRetries), cancellationToken);
                        return;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(secondsBetweenRetries), cancellationToken);
                count++;
            }
            throw new TimeoutException("Leader not initialized after " + count * secondsBetweenRetries + " seconds" );
        }

    }
}

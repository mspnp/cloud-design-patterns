using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Shared.Service;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System.Configuration;
using System.Linq;

namespace WorkerStatelessService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class WorkerStatelessService : StatelessService
    {
        private static readonly string LeaderServiceName = ConfigurationManager.AppSettings["LeaderServiceName"];
        private static readonly int secondsBetweenRetries = 10;
        private static readonly int numberOfRetries = 12;

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

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();
            if(count == numberOfRetries)
                throw new TimeoutException("Leader not initialized after " + count * secondsBetweenRetries + " seconds" );

            var fabricClient = new FabricClient();
            var apps = fabricClient.QueryManager.GetApplicationListAsync().Result;
            
            if (apps.Any())
            {
                var services = await fabricClient.QueryManager.GetServiceListAsync(apps.First().ApplicationName);
                if (services.All(s => s.ServiceName.LocalPath != leaderServiceName))
                {
                    await Wait(cancellationToken, count);
                }
            }
            else
            {
                await Wait(cancellationToken, count);
            }
        }

        private static async Task Wait(CancellationToken cancellationToken, int count)
        {
            await Task.Delay(TimeSpan.FromSeconds(secondsBetweenRetries*count), cancellationToken);
            count++;
            await WaitForLeader(LeaderServiceName, cancellationToken, count);
        }
    }
}

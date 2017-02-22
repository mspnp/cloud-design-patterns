using System;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderElectionTest
{
    class Program
    {
        public static int Main(string[] args)
        {
            var clusterConnection = ConfigurationManager.AppSettings["clusterConnection"];
            var serviceName = new Uri(ConfigurationManager.AppSettings["serviceName"]);

            Console.WriteLine("Starting leader node restart test");
            try
            {
                //Restart the leader (primary node of LeaderStatefulService)
                RestartNodeAsync(clusterConnection, serviceName).Wait();
            }
            catch (AggregateException exAgg)
            {
                Console.WriteLine("Restart leader node did not complete: ");
                foreach (FabricException ex in exAgg.InnerExceptions.OfType<FabricException>())
                {
                    Console.WriteLine("HResult: {0} Message: {1}", ex.HResult, ex.Message);
                }
                return -1;
            }

            Console.WriteLine("Leader node restarted successfully.");
            return 0;
        }

        static async Task RestartNodeAsync(string clusterConnection, Uri serviceName)
        {
            var randomPartitionSelector = PartitionSelector.RandomOf(serviceName);
            var primaryofReplicaSelector = ReplicaSelector.PrimaryOf(randomPartitionSelector);

            var fabricclient = new FabricClient(clusterConnection);
            await fabricclient.FaultManager.RestartNodeAsync(primaryofReplicaSelector, CompletionMode.Verify);
        }
    }
}

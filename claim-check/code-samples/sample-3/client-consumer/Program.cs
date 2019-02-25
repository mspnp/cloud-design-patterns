using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ClientConsumer
{
    public class Program
    {
        private static async Task MessageLoop(object state) {
            (var consumer, var cancellationToken) = (ValueTuple<IConsumer, CancellationToken>)state;
            while (!cancellationToken.IsCancellationRequested)
            {
                await consumer.ProcessMessages(cancellationToken);
            }
        }

        public static async Task Main(string[] args)
        {
            IConsumer consumer = new ServiceBusAttachmentConsumer();
            consumer.Configure();

            Console.WriteLine("Receiving messages...");
            var cts = new CancellationTokenSource();
            var task = Task.Factory.StartNew(MessageLoop,
                (consumer, cts.Token),
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );

            Console.WriteLine("Press any key to terminate the application...");
            Console.ReadKey(true);
            cts.Cancel();

            Console.WriteLine("Exiting...");
            await task;

            Console.WriteLine("Done.");
        }
    }
}

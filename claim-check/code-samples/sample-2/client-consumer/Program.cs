using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClientConsumer
{
    public class Program
    {
        private static async Task MessageLoop(object state) {
            (var consumer, var cancellationToken) = (ValueTuple<IReader, CancellationToken>)state;
            while (!cancellationToken.IsCancellationRequested)
            {
                await consumer.ProcessMessages(cancellationToken);
            }
        }

        public static async Task Main(string[] args)
        {
            IReader reader = new EventReader();
            reader.Configure();

            Console.WriteLine("Dequeuing messages...");
            var cts = new CancellationTokenSource();
            var task = Task.Factory.StartNew(MessageLoop,
                (reader, cts.Token),
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

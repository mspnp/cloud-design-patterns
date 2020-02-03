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
            (var reader, var cancellationToken) = (ValueTuple<IReader, CancellationToken>)state;
            while (!cancellationToken.IsCancellationRequested)
            {
                await reader.ProcessMessages(cancellationToken);
            }
        }

        public static async Task Main(string[] args)
        {
            IReader reader = new EventReader();
            reader.Configure();

            Console.WriteLine("Dequeuing messages...");
            var cts = new CancellationTokenSource();
            var task = Task.Run(async () =>
            {
                await MessageLoop((reader, cts.Token));
            });

            Console.WriteLine("Press any key to terminate the application...");
            Console.ReadKey(true);
            cts.Cancel();

            Console.WriteLine("Exiting...");
            await task;

            Console.WriteLine("Done.");
        }
    }
}

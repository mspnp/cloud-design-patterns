// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LeaderElection
{
    using System;
    using System.Configuration;
    using static System.Configuration.ConfigurationManager;
    using System.Threading;
    using System.Threading.Tasks;
    using DistributedMutex;

    class Program
    {
        static async Task Main(string[] args)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // Get the connection string from app settings
            var storageConnStr = ConfigurationManager.AppSettings["StorageConnectionString"];
            var pid = System.Diagnostics.Process.GetCurrentProcess().Id;

            if (string.IsNullOrEmpty(storageConnStr))
            {
                Console.WriteLine("A connection string must be set in the app.config file.");
                return;
            }
            // Create a BlobSettings object with the connection string and the name of the blob to use for the lease
            BlobSettings blobSettings = new BlobSettings(
                storageConnStr,
                "leases",
                "leader");

            DistributedMutex.BlobDistributedMutex distributedMutex = new DistributedMutex.BlobDistributedMutex(
                blobSettings,
                async (CancellationToken token) =>
                {
                    var lastUpdate = DateTime.Now.AddSeconds(-16);
                    while (!token.IsCancellationRequested)
                    {
                        if(DateTime.Now > lastUpdate.AddSeconds(15))
                        {
                            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] This process ({pid}) is currently the leader. Press any key to exit.");
                            lastUpdate = DateTime.Now;
                        }
                        // Check for a keypress to stop being the leader
                        await Task.Delay(1000);
                        if (Console.KeyAvailable)
                        {
                            Console.WriteLine("Stopping this process. Leader position will be released.");
                            Console.ReadKey(true);
                            source.Cancel();
                        }
                    }
                }, () => {
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] This process ({pid}) could not acquire lease. Retrying in 20 seconds.");
                });
            await distributedMutex.RunTaskWhenMutexAcquired(token);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LeaderElectionConsoleWorker
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
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
                Console.Error.WriteLine("A connection string must be set in the app.config file.");
                return;
            }
            // Create a BlobSettings object with the connection string and the name of the blob to use for the lease
            BlobSettings blobSettings = new BlobSettings(
                storageConnStr,
                "leases",
                "leader");

            // Start an async task that will wait for a keypress and cancel the token when a key is pressed
            var uiTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Requesting shutdown.");
                        source.Cancel();
                    }
                    await Task.Delay(500);
                }
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] This process ({pid}) is shutting down.");

            });

            DistributedMutex.BlobDistributedMutex distributedMutex = new DistributedMutex.BlobDistributedMutex(
                blobSettings,
                async (CancellationToken token) =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] This process ({pid}) is currently the leader. Press any key to exit.");
                        await Task.Delay(15000);
                    }
                }, () => {
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] This process ({pid}) could not acquire lease. Retrying in 20 seconds. Press any key to exit.");
                });
            await distributedMutex.RunTaskWhenMutexAcquired(token);
            await uiTask;
        }
    }
}

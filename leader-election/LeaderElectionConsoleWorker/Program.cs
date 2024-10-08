// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LeaderElectionConsoleWorker
{
    using System;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;
    using DistributedMutex;

    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a new shared cancellation token source
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // Get the connection string from app settings
            var storageUri = ConfigurationManager.AppSettings["StorageUri"];
            if (string.IsNullOrEmpty(storageUri))
            {
                Console.Error.WriteLine("A connection string must be set in the app.config file.");
                return;
            }
            // Create a BlobSettings object with the connection string and the name of the blob to use for the lease
            BlobSettings blobSettings = new BlobSettings(
                storageUri,
                "leases",
                "leader");

            // Get the current process ID for output
            var pid = System.Diagnostics.Process.GetCurrentProcess().Id;

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

            // Create a new BlobDistributedMutex object with the BlobSettings object and a task
            // to run when the lease is acquired, and an action to run when the lease is not acquired.
            BlobDistributedMutex distributedMutex = new BlobDistributedMutex(
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

            // Wait for completion of the DistributedMutex and the UI task before exiting
            await distributedMutex.RunTaskWhenMutexAcquired(token);
            await uiTask;
        }
    }
}

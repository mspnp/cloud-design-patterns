// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace LeaderElection.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DistributedMutex;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;

    [TestClass]
    public class DistributedMutexFixture
    {
        [TestMethod]
        public void OnlyOneMutexStartsTask()
        {
            const int ConcurrentMutexes = 5;
            var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "leases", "OnlyOneMutexStartsTask");

            var mutexAcquired = Enumerable.Range(0, ConcurrentMutexes).Select(_ => new TaskCompletionSource<bool>()).ToArray();

            var mutexes = mutexAcquired.Select(completed => new BlobDistributedMutex(settings, SignalAndWait(completed))).ToArray();

            var cts = new CancellationTokenSource();

            foreach (var mutex in mutexes)
            {
                mutex.RunTaskWhenMutexAcquired(cts.Token);
            }

            bool allFinished = Task.WaitAll(mutexAcquired.Select(x => (Task)x.Task).ToArray(), TimeSpan.FromSeconds(10));

            cts.Cancel();

            Assert.IsFalse(allFinished);
            Assert.AreEqual(1, mutexAcquired.Count(x => x.Task.IsCompleted));
        }

        [TestMethod]
        public void LeaderRenewsLease()
        {
            const int ConcurrentMutexes = 5;
            var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "leases", "LeaderRenewsLease");

            var mutexAcquired = Enumerable.Range(0, ConcurrentMutexes).Select(_ => new TaskCompletionSource<bool>()).ToArray();

            var mutexes = mutexAcquired.Select(completed => new BlobDistributedMutex(settings, SignalAndWait(completed))).ToArray();

            var cts = new CancellationTokenSource();

            foreach (var mutex in mutexes)
            {
                mutex.RunTaskWhenMutexAcquired(cts.Token);
            }

            bool allFinished = Task.WaitAll(mutexAcquired.Select(x => (Task)x.Task).ToArray(), TimeSpan.FromMinutes(3));

            cts.Cancel();

            Assert.IsFalse(allFinished);
            Assert.AreEqual(1, mutexAcquired.Count(x => x.Task.IsCompleted));
        }

        [TestMethod]
        public void LeaderAbortingCreatesNewLeader()
        {
            const int ConcurrentMutexes = 5;
            var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "leases", "LeaderAbortingCreatesNewLeader");

            var firstCts = new CancellationTokenSource();
            var firstMutexAcquired = new TaskCompletionSource<bool>();
            var firstLeader = new BlobDistributedMutex(settings, SignalAndWait(firstMutexAcquired));
            firstLeader.RunTaskWhenMutexAcquired(firstCts.Token);

            Assert.IsTrue(firstMutexAcquired.Task.Wait(TimeSpan.FromSeconds(5)));

            var mutexAcquired = Enumerable.Range(0, ConcurrentMutexes).Select(_ => new TaskCompletionSource<bool>()).ToArray();

            var mutexes = mutexAcquired.Select(completed => new BlobDistributedMutex(settings, SignalAndWait(completed))).ToArray();

            var cts = new CancellationTokenSource();

            foreach (var mutex in mutexes)
            {
                mutex.RunTaskWhenMutexAcquired(cts.Token);
            }

            firstCts.Cancel();

            Task.WaitAny(mutexAcquired.Select(x => (Task)x.Task).ToArray(), TimeSpan.FromSeconds(80));

            cts.Cancel();

            Assert.AreEqual(1, mutexAcquired.Count(x => x.Task.IsCompleted));
        }

        private static Func<CancellationToken, Task> SignalAndWait(TaskCompletionSource<bool> signal)
        {
            return async token => { signal.SetResult(true); await Task.Delay(1000000, token); };
        }
    }
}

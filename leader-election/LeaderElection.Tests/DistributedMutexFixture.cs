// ==============================================================================================================
// Microsoft patterns & practices
// Cloud Design Patterns project
// ==============================================================================================================
// ©2013 Microsoft. All rights reserved. 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================
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

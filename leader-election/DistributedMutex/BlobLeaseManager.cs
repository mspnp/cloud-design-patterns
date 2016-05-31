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
namespace DistributedMutex
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;

    public struct BlobSettings
    {
        public readonly string Container;
        public readonly string BlobName;
        public CloudStorageAccount StorageAccount;

        public BlobSettings(CloudStorageAccount storageAccount, string container, string blobName)
        {
            this.StorageAccount = storageAccount;
            this.Container = container;
            this.BlobName = blobName;
        }
    }

    /// <summary>
    /// Wrapper around a Windows Azure Blob Lease
    /// </summary>
    internal class BlobLeaseManager
    {
        private readonly CloudPageBlob leaseBlob;

        public BlobLeaseManager(BlobSettings settings)
            : this(settings.StorageAccount.CreateCloudBlobClient(), settings.Container, settings.BlobName)
        {
        }

        public BlobLeaseManager(CloudBlobClient blobClient, string leaseContainerName, string leaseBlobName)
        {
            blobClient.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 3);
            var container = blobClient.GetContainerReference(leaseContainerName);
            this.leaseBlob = container.GetPageBlobReference(leaseBlobName);
        }

        public void ReleaseLease(string leaseId)
        {
            try
            {
                this.leaseBlob.ReleaseLease(new AccessCondition { LeaseId = leaseId });
            }
            catch (StorageException e)
            {
                // Lease will eventually be released.
                Trace.TraceError(e.Message);
            }
        }

        public async Task<string> AcquireLeaseAsync(CancellationToken token)
        {
            bool blobNotFound = false;
            try
            {
                return await this.leaseBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(60), null, token);
            }
            catch (StorageException storageException)
            {
                Trace.TraceError(storageException.Message);

                var webException = storageException.InnerException as WebException;

                if (webException != null)
                {
                    var response = webException.Response as HttpWebResponse;
                    if (response != null)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            blobNotFound = true;
                        }

                        if (response.StatusCode == HttpStatusCode.Conflict)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            if (blobNotFound)
            {
                await this.CreateBlobAsync(token);
                return await this.AcquireLeaseAsync(token);
            }

            return null;
        }

        public async Task<bool> RenewLeaseAsync(string leaseId, CancellationToken token)
        {
            try
            {
                await this.leaseBlob.RenewLeaseAsync(new AccessCondition { LeaseId = leaseId }, token);
                return true;
            }
            catch (StorageException storageException)
            {
                // catch (WebException webException)
                Trace.TraceError(storageException.Message);

                return false;
            }
        }

        private async Task CreateBlobAsync(CancellationToken token)
        {
            await this.leaseBlob.Container.CreateIfNotExistsAsync(token);
            if (!await this.leaseBlob.ExistsAsync(token))
            {
                try
                {
                    await this.leaseBlob.CreateAsync(0, token);
                }
                catch (StorageException e)
                {
                    if (e.InnerException is WebException)
                    {
                        var webException = e.InnerException as WebException;
                        var response = webException.Response as HttpWebResponse;

                        if (response == null || response.StatusCode != HttpStatusCode.PreconditionFailed)
                        {
                            throw;
                        }
                    }
                }
            }
        }
    }
}
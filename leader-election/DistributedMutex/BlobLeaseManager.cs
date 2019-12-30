// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DistributedMutex
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Specialized;

    public struct BlobSettings
    {
        public readonly string Container;
        public readonly string BlobName;
        public BlobServiceClient StorageAccount;


        public BlobSettings(String storageConnStr, string container, string blobName)
        {
            var blobOption = new BlobClientOptions();
            blobOption.Retry.Delay = TimeSpan.FromSeconds(5);
            blobOption.Retry.MaxRetries = 3;
            
            this.StorageAccount = new BlobServiceClient(storageConnStr, blobOption);
            this.Container = container;
            this.BlobName = blobName;
        }
    }

    /// <summary>
    /// Wrapper around a Windows Azure Blob Lease
    /// </summary>
    internal class BlobLeaseManager
    {
        //private readonly CloudPageBlob leaseBlob;
        private readonly BlobContainerClient leaseContainerClient;
        private readonly PageBlobClient leaseBlobClient;
        private BlobLeaseClient leaseClient;

        public BlobLeaseManager(BlobSettings settings)
            : this(settings.StorageAccount, settings.Container, settings.BlobName)
        {
        }

        public BlobLeaseManager(BlobServiceClient storageBlob, string leaseContainerName, string leaseBlobName)
        {
            this.leaseContainerClient = storageBlob.GetBlobContainerClient(leaseContainerName);
            this.leaseBlobClient = this.leaseContainerClient.GetPageBlobClient(leaseBlobName);
            
        }

        public void ReleaseLease(string leaseId)
        {
            try
            {
                //this.leaseBlob.ReleaseLease(new AccessCondition { LeaseId = leaseId });
                var leaseClient = this.leaseBlobClient.GetBlobLeaseClient(leaseId);
                leaseClient.Release();

                
            }
            catch (Azure.RequestFailedException e) // use specific exception class of Storage Blob Track2
            {
                // Lease will eventually be released.
                Trace.TraceError(e.ErrorCode);
            }
        }

        public async Task<string> AcquireLeaseAsync(CancellationToken token)
        {
            bool blobNotFound = false;
            try
            {
                //return await this.leaseBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(60), null, token);
                var leaseClient = this.leaseBlobClient.GetBlobLeaseClient();
                var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(60), null, token);
                return lease.Value.LeaseId;
            }
            catch (Azure.RequestFailedException storageException) // use specific exception class of Storage Blob Track2
            {
                Trace.TraceError(storageException.ErrorCode);

                var status = storageException.Status;
                if (status == (int) HttpStatusCode.NotFound)
                {
                    blobNotFound = true;
                }

                if (status == (int)HttpStatusCode.Conflict)
                {
                    return null;
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
                if (this.leaseClient == null)
                {
                    this.leaseClient = this.leaseBlobClient.GetBlobLeaseClient(leaseId);
                }

                await this.leaseClient.RenewAsync(cancellationToken: token);
                return true;
            }
            catch (Azure.RequestFailedException storageException) // use specific exception class for StorageBlob Track2
            {
                // catch (WebException webException)
                Trace.TraceError(storageException.ErrorCode);

                return false;
            }
        }

        private async Task CreateBlobAsync(CancellationToken token)
        {
            //await this.leaseBlob.Container.CreateIfNotExistsAsync(token);
            await this.leaseContainerClient.CreateIfNotExistsAsync(cancellationToken: token);

            try
            {
                await this.leaseBlobClient.CreateIfNotExistsAsync(0, cancellationToken: token);
            }
            catch (Exception e) // use specific exception class for StorageBlob Track2
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
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DistributedMutex
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Specialized;

    public struct BlobSettings
    {
        public readonly string Container;
        public readonly string BlobName;
        public BlobServiceClient BlobServiceClient;
        public readonly int LeaseTime;
        public readonly int RenewInterval;


        public BlobSettings(String storageConnStr, string container, string blobName)
        {
            var blobClientOptions = new BlobClientOptions();
            blobClientOptions.Retry.Delay = TimeSpan.FromSeconds(5);
            blobClientOptions.Retry.MaxRetries = 3;

            this.BlobServiceClient = new BlobServiceClient(storageConnStr, blobClientOptions);
            this.Container = container;
            this.BlobName = blobName;
        }
    }

    /// <summary>
    /// Wrapper around a Windows Azure Blob Lease
    /// </summary>
    internal class BlobLeaseManager
    {
        private readonly BlobContainerClient leaseContainerClient;
        private readonly PageBlobClient leaseBlobClient;

        public BlobLeaseManager(BlobSettings settings)
            : this(settings.BlobServiceClient, settings.Container, settings.BlobName)
        {
        }

        public BlobLeaseManager(BlobServiceClient blobServiceClient, string leaseContainerName, string leaseBlobName)
        {
            this.leaseContainerClient = blobServiceClient.GetBlobContainerClient(leaseContainerName);
            this.leaseBlobClient = this.leaseContainerClient.GetPageBlobClient(leaseBlobName);
        }

        public void ReleaseLease(string leaseId)
        {
            try
            {
                var leaseClient = this.leaseBlobClient.GetBlobLeaseClient(leaseId);
                leaseClient.Release();
            }
            catch (RequestFailedException e)
            {
                // Lease will eventually be released.
                Trace.TraceError(e.ErrorCode);
            }
        }

        public async Task<string?> AcquireLeaseAsync(CancellationToken token)
        {
            bool blobNotFound = false;
            try
            {
                var leaseClient = this.leaseBlobClient.GetBlobLeaseClient();
                var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(15), null, token);
                return lease.Value.LeaseId;
            }
            catch (RequestFailedException storageException)
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
                var leaseClient = this.leaseBlobClient.GetBlobLeaseClient(leaseId);
                await leaseClient.RenewAsync(cancellationToken: token);
                return true;
            }
            catch (RequestFailedException storageException)
            {
                Trace.TraceError(storageException.ErrorCode);

                return false;
            }
        }

        private async Task CreateBlobAsync(CancellationToken token)
        {
            await this.leaseContainerClient.CreateIfNotExistsAsync(cancellationToken: token);
            await this.leaseBlobClient.CreateIfNotExistsAsync(0, cancellationToken: token);
        }
    }
}
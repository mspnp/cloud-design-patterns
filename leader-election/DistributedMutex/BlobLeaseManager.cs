// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace DistributedMutex;

public readonly record struct BlobSettings(string BlobUri, string Container, string BlobName)
{
    public BlobServiceClient CreateBlobServiceClient()
    {
        var options = new BlobClientOptions();
        options.Retry.Delay = TimeSpan.FromSeconds(5);
        options.Retry.MaxRetries = 3;

        return new BlobServiceClient(new Uri(BlobUri), new DefaultAzureCredential(), options);
    }
}

/// <summary>
/// Wrapper around a Windows Azure Blob Lease
/// </summary>
internal class BlobLeaseManager
{
    private readonly BlobContainerClient leaseContainerClient;
    private readonly PageBlobClient leaseBlobClient;

    private const int LeaseAcquireTimeoutSeconds = 15;
    private const int LeaseAlreadyPresentStatusCode = 412;

    private BlobLeaseManager(BlobServiceClient blobServiceClient, string leaseContainerName, string leaseBlobName)
    {
        leaseContainerClient = blobServiceClient.GetBlobContainerClient(leaseContainerName);
        leaseBlobClient = leaseContainerClient.GetPageBlobClient(leaseBlobName);
    }

    public static async Task<BlobLeaseManager> CreateAsync(BlobSettings settings, CancellationToken token = default)
    {
        var client = settings.CreateBlobServiceClient();
        var manager = new BlobLeaseManager(client, settings.Container, settings.BlobName);
        await manager.InitializeAsync(token);
        return manager;
    }

    private async Task InitializeAsync(CancellationToken token)
    {
        await leaseContainerClient.CreateIfNotExistsAsync(cancellationToken: token);
        try
        {
            await leaseBlobClient.CreateIfNotExistsAsync(512, cancellationToken: token);
        }
        catch (RequestFailedException leaseAlreadyPresentException)
        {
            // There is currently a lease on the blob and no lease ID was specified in the request.
            // Status=412. It throws an Exception if the lease exists and is already taken.
            if (leaseAlreadyPresentException.Status != LeaseAlreadyPresentStatusCode) throw;
        }
    }

    public void ReleaseLease(string leaseId)
    {
        try
        {
            var leaseClient = leaseBlobClient.GetBlobLeaseClient(leaseId);
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
            var leaseClient = leaseBlobClient.GetBlobLeaseClient();
            var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(LeaseAcquireTimeoutSeconds), null, token);
            return lease.Value.LeaseId;
        }
        catch (RequestFailedException storageException)
        {
            Trace.TraceError(storageException.ErrorCode);

            var status = storageException.Status;
            if (status == (int)HttpStatusCode.NotFound)
            {
                blobNotFound = true;
            }

            if (status == (int)HttpStatusCode.Conflict)
            {
                return null;
            }
        }
        catch (Exception e)
        {
            // If the storage account is unavailable or we fail for any other reason we still want to keep retrying
            Trace.TraceError(e.Message);
            return null;
        }

        if (blobNotFound)
        {
            await CreateBlobAsync(token);
            return await AcquireLeaseAsync(token);
        }

        return null;
    }

    public async Task<bool> RenewLeaseAsync(string leaseId, CancellationToken token)
    {
        try
        {
            var leaseClient = leaseBlobClient.GetBlobLeaseClient(leaseId);
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
        await leaseContainerClient.CreateIfNotExistsAsync(cancellationToken: token);
        await leaseBlobClient.CreateIfNotExistsAsync(0, cancellationToken: token);
    }
}
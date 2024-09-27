using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Asyncpattern
{
    public class AsyncOperationStatusChecker(ILogger<AsyncOperationStatusChecker> _logger)
    {  
        [Function("AsyncOperationStatusChecker")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RequestStatus/{thisGUID}")] HttpRequest req,
             [BlobInput("data/{thisGUID}.blobdata", Connection = "DataStorage")] BlockBlobClient inputBlob, string thisGUID)
        {
            OnCompleteEnum OnComplete = Enum.Parse<OnCompleteEnum>(req.Query["OnComplete"].FirstOrDefault() ?? "Redirect");
            OnPendingEnum OnPending = Enum.Parse<OnPendingEnum>(req.Query["OnPending"].FirstOrDefault() ?? "OK");

            _logger.LogInformation($"C# HTTP trigger function processed a request for status on {thisGUID} - OnComplete {OnComplete} - OnPending {OnPending}");

            // ** Check to see if the blob is present **
            if (await inputBlob.ExistsAsync())
            {
                // ** If it's present, depending on the value of the optional "OnComplete" parameter choose what to do. **
                return await OnCompleted(OnComplete, inputBlob, thisGUID);
            }
            else
            {
                // ** If it's NOT present, then we need to back off, so depending on the value of the optional "OnPending" parameter choose what to do. **
                string rqs = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/RequestStatus/{thisGUID}";

                switch (OnPending)
                {
                    case OnPendingEnum.OK:
                        {
                            // Return an HTTP 200 status code with the 
                            return new OkObjectResult(new { status = "In progress", Location = rqs });
                        }

                    case OnPendingEnum.Synchronous:
                        {
                            // Back off and retry. Time out if the backoff period hits one minute
                            int backoff = 250;

                            while (!await inputBlob.ExistsAsync() && backoff < 64000)
                            {
                                _logger.LogInformation($"Synchronous mode {thisGUID}.blob - retrying in {backoff} ms");
                                backoff = backoff * 2;
                                await Task.Delay(backoff);
                            }

                            if (await inputBlob.ExistsAsync())
                            {
                                _logger.LogInformation($"Synchronous Redirect mode {thisGUID}.blob - completed after {backoff} ms");
                                return await OnCompleted(OnComplete, inputBlob, thisGUID);
                            }
                            else
                            {
                                _logger.LogInformation($"Synchronous mode {thisGUID}.blob - NOT FOUND after timeout {backoff} ms");
                                return new NotFoundResult();
                            }
                        }

                    default:
                        {
                            throw new InvalidOperationException($"Unexpected value: {OnPending}");
                        }
                }
            }
        }
        private async Task<IActionResult> OnCompleted(OnCompleteEnum OnComplete, BlockBlobClient inputBlob, string thisGUID)
        {
            switch (OnComplete)
            {
                case OnCompleteEnum.Redirect:

                    {
                        // The typical way to generate a SAS token in code requires the storage account key.
                        //If you need to use “Managed Identity” to control access to your storage accounts in code, which is something I highly recommend wherever possible as this is a security best practice.
                        // In this scenario, you won't have a storage account key, so you'll need to find another way to generate the shared access signatures.
                        // To do that, we need to use an approach called user delegation SAS . By using a user delegation SAS, we can sign the signature with the Microsoft Entra ID credentials instead of the storage account key.
                        BlobServiceClient blobServiceClient = inputBlob.GetParentBlobContainerClient().GetParentBlobServiceClient();
                        var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
                        // Redirect to the SAS URI to blob storage
                        return new RedirectResult(inputBlob.GenerateSASURI(userDelegationKey));
                    }

                case OnCompleteEnum.Stream:
                    {
                        // Download the file and return it directly to the caller.
                        // For larger files, use a stream to minimize RAM usage.
                        return new OkObjectResult(await inputBlob.DownloadContentAsync());
                    }

                default:
                    {
                        throw new InvalidOperationException($"Unexpected value: {OnComplete}");
                    }
            }
        }
    }

    public enum OnCompleteEnum
    {

        Redirect,
        Stream
    }

    public enum OnPendingEnum
    {

        OK,
        Synchronous
    }
}

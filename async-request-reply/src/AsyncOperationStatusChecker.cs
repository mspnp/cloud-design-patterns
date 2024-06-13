using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace asyncpattern
{
    public class AsyncOperationStatusChecker
    {
        private readonly ILogger<AsyncOperationStatusChecker> _logger;

        public AsyncOperationStatusChecker(ILogger<AsyncOperationStatusChecker> logger)
        {
            _logger = logger;
        }

        [Function("AsyncOperationStatusChecker")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RequestStatus/{thisGUID}")] HttpRequest req,
             [BlobInput("data/{thisGUID}.blobdata",Connection = "StorageConnectionAppSetting")] BlockBlobClient inputBlob, string thisGUID)
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
                        // Redirect to the SAS URI to blob storage

                        return new RedirectResult(inputBlob.GenerateSASURI());
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

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

namespace Microsoft.PnP.Messaging
{
    public static class ClaimCheck
    {    
        [FunctionName("ClaimCheck")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestContent = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"Received events: {requestContent}");

            EventGridEvent[] eventGridEvents = EventGridEvent.ParseMany(BinaryData.FromStream(req.Body));

            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                eventGridEvent.TryGetSystemEventData(out object systemEvent);
                switch (systemEvent)
                {
                    case SubscriptionValidationEventData subscriptionValidation:
                        log.LogInformation($"Got SubscriptionValidation event data, validationCode: {subscriptionValidation.ValidationCode},  validationUrl: {subscriptionValidation.ValidationUrl}, topic: {eventGridEvent.Topic}");
                        // Do any additional validation (as required) such as validating that the Azure resource ID of the topic matches
                        // the expected topic and then return back the below response
                        break;
                    case StorageBlobCreatedEventData storageBlobCreated:
                        log.LogInformation($"Got BlobCreated event data, blob URI {storageBlobCreated.Url}");
                        break;
                    // Handle other messages here
                    default:
                        return new BadRequestResult();
                }
            }
            
            return new OkResult();
        }
    }
}

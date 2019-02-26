using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;

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

            EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();
            EventGridEvent[] eventGridEvents = eventGridSubscriber.DeserializeEventGridEvents(requestContent);

            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                switch (eventGridEvent.Data) {
                    case SubscriptionValidationEventData subscriptionValidation:
                        log.LogInformation($"Got SubscriptionValidation event data, validationCode: {subscriptionValidation.ValidationCode},  validationUrl: {subscriptionValidation.ValidationUrl}, topic: {eventGridEvent.Topic}");
                        // Do any additional validation (as required) such as validating that the Azure resource ID of the topic matches
                        // the expected topic and then return back the below response
                        return new OkObjectResult(new SubscriptionValidationResponse()
                        {
                            ValidationResponse = subscriptionValidation.ValidationCode
                        });
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

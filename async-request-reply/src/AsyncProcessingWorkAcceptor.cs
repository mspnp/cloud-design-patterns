using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Asyncpattern
{
    public class AsyncProcessingWorkAcceptor(ILogger<AsyncProcessingWorkAcceptor> _logger, ServiceBusClient _serviceBusClient)
    {
        [Function("AsyncProcessingWorkAcceptor")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, [FromBody] CustomerPOCO customer)
        {
            if (string.IsNullOrEmpty(customer.id) || string.IsNullOrEmpty(customer.customername))
            {
                return new BadRequestResult();
            }

            var reqid = Guid.NewGuid().ToString();

            var rqs = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/RequestStatus/{reqid}";

            var messagePayload = JsonConvert.SerializeObject(customer);
            var message = new ServiceBusMessage(messagePayload);
            message.ApplicationProperties.Add("RequestGUID", reqid);
            message.ApplicationProperties.Add("RequestSubmittedAt", DateTime.Now);
            message.ApplicationProperties.Add("RequestStatusURL", rqs);
            var sender = _serviceBusClient.CreateSender("outqueue");

            await sender.SendMessageAsync(message);
            return new AcceptedResult(rqs, $"Request Accepted for Processing{Environment.NewLine}ProxyStatus: {rqs}");
        }
    }
}

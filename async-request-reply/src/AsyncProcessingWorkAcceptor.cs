using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Contoso
{
    public static class AsyncProcessingWorkAcceptor
    {
        [FunctionName("AsyncProcessingWorkAcceptor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] CustomerPOCO customer,
            [ServiceBus("outqueue", Connection = "ServiceBusConnectionAppSetting")] IAsyncCollector<ServiceBusMessage> OutMessages,
            ILogger log)
        {
            if (String.IsNullOrEmpty(customer.id) || string.IsNullOrEmpty(customer.customername))
            {
                return new BadRequestResult();
            }

            string reqid = Guid.NewGuid().ToString();
            
            string rqs = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/RequestStatus/{reqid}";

            var messagePayload = JsonConvert.SerializeObject(customer);
            var message = new ServiceBusMessage(messagePayload);
            message.ApplicationProperties.Add("RequestGUID", reqid);
            message.ApplicationProperties.Add("RequestSubmittedAt", DateTime.Now);
            message.ApplicationProperties.Add("RequestStatusURL", rqs);

            await OutMessages.AddAsync(message);

            return new AcceptedResult(rqs, $"Request Accepted for Processing{Environment.NewLine}ProxyStatus: {rqs}");
        }
    }
}

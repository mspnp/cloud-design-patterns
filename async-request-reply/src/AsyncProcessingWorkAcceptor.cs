using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.ServiceBus;
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
            if (String.IsNullOrEmpty(customer.id) || String.IsNullOrEmpty(customer.customername))
            {
                return new BadRequestResult();
            }

            string reqid = Guid.NewGuid().ToString();
            
            string rqs = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/RequestStatus/{reqid}";

            var messagePayload = JsonConvert.SerializeObject(customer);
            var message = new ServiceBusMessage(messagePayload);
            m.ApplicationProperties["RequestGUID"] = reqid;
            m.ApplicationProperties["RequestSubmittedAt"] = DateTime.Now;
            m.ApplicationProperties["RequestStatusURL"] = rqs;
                
            await OutMessages.AddAsync(message);

            return (ActionResult) new AcceptedResult(rqs, $"Request Accepted for Processing{Environment.NewLine}ProxyStatus: {rqs}");
        }
    }
}

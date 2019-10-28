using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
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
            [ServiceBus("outqueue", Connection = "ServiceBusConnectionAppSetting")] IAsyncCollector<Message> OutMessage,
            ILogger log)
        {
            if (String.IsNullOrEmpty(customer.id) || String.IsNullOrEmpty(customer.customername))
            {
                return new BadRequestResult();
            }

            string reqid = Guid.NewGuid().ToString();
            
            string rqs = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/RequestStatus/{reqid}";

            var messagePayload = JsonConvert.SerializeObject(customer);
            Message m = new Message(Encoding.UTF8.GetBytes(messagePayload));
            m.UserProperties["RequestGUID"] = reqid;
            m.UserProperties["RequestSubmittedAt"] = DateTime.Now;
            m.UserProperties["RequestStatusURL"] = rqs;
                
            await OutMessage.AddAsync(m);  

            return (ActionResult) new AcceptedResult(rqs, $"Request Accepted for Processing{Environment.NewLine}ProxyStatus: {rqs}");  
        }
    }
}

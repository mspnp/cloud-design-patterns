using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System.Text;

namespace Contoso
{
    public static class AsyncProcessingWorkAcceptor
    {
        [FunctionName("AsyncProcessingWorkAcceptor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] CustomerPOCO customer,
            [Blob("data", FileAccess.Read, Connection = "StorageConnectionAppSetting")] CloudBlobContainer inputBlob,
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

            CloudBlockBlob cbb = inputBlob.GetBlockBlobReference($"{reqid}.blobdata");
            var sasUri = cbb.GenerateSASURI();
            return (ActionResult) new AcceptedResult(rqs, $"Request Accepted for Processing{Environment.NewLine}ValetKey: {sasUri}{Environment.NewLine}ProxyStatus: {rqs}");  
        }
    }
}

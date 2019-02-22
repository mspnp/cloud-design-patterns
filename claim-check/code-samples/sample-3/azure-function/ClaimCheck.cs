using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus.Core;
using ServiceBus.AttachmentPlugin;
using System.Text;
using Microsoft.Azure.ServiceBus;

namespace Microsoft.PnP.Messaging
{
    public static class ClaimCheck
    {
        [FunctionName("ClaimCheck")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            //log.LogInformation("C# HTTP trigger function processed a request.");

            string requestContent = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"Received payload from client. Sending to Service Bus as Attachment.");

            // Getting connection information
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING");
            var queueName = Environment.GetEnvironmentVariable("QUEUE_NAME");
            var storageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");

            // Creating config for sending message
            var config = new AzureStorageAttachmentConfiguration(storageConnectionString);

            // Creating and registering the sender using Service Bus Connection String and Queue Name
            var sender = new MessageSender(serviceBusConnectionString, queueName);
            sender.RegisterAzureStorageAttachmentPlugin(config);
            
            // Create payload
            var payload = new { data = requestContent };
            var serialized = JsonConvert.SerializeObject(payload);
            var payloadAsBytes = Encoding.UTF8.GetBytes(serialized);
            var message = new Message(payloadAsBytes);

            // Send the message
            await sender.SendAsync(message);
            return new OkObjectResult($"Message is sent!");
        }
    }
}

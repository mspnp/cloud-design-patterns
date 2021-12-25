using Azure.Messaging.ServiceBus;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PriorityQueueSender
{
    public static class PriorityQueueSenderFn
    {
        private static QueueManager queueManager;

        [FunctionName("PriorityQueueSenderFunction")]
        public async static Task Run([TimerTrigger("0,30 * * * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
        {
            // Make sure you provide the corresponding Service Bus connection information in the service configuration file.
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            var topicName = Environment.GetEnvironmentVariable("TopicName");

            queueManager = new QueueManager(serviceBusConnectionString, topicName);
            await queueManager.SetupTopic();

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                var lowMessages = new List<ServiceBusMessage>();

                for (int i = 0; i < 10; i++)
                {
                    var messageId = Guid.NewGuid().ToString();
                    var message = new ServiceBusMessage() { MessageId = messageId };
                    message.ApplicationProperties["Priority"] = Priority.Low;
                    message.Body = BinaryData.FromString($"Low priority message with Id: {messageId}");
                    lowMessages.Add(message);
                }

                queueManager.SendBatchAsync(lowMessages).Wait();
                log.LogInformation("Sent low priority message batch");

                var highMessages = new List<ServiceBusMessage>();

                for (int i = 0; i < 10; i++)
                {
                    var messageId = Guid.NewGuid().ToString();
                    var message = new ServiceBusMessage() { MessageId = messageId };
                    message.ApplicationProperties["Priority"] = Priority.High;
                    message.Body = BinaryData.FromString($"High priority message with Id: {messageId}");
                    highMessages.Add(message);
                }

                queueManager.SendBatchAsync(highMessages).Wait();
                log.LogInformation("Sent high priority message batch");

                queueManager.StopSender().Wait();
            }
            catch (Exception exc)
            {
                log.LogError("Exception in initial sender: {0}", exc.Message);

                // Avoid the situation where a configuration error or some other long term exception causes us to fill up the logs
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;

namespace PriorityQueueSender
{
    public static class PriorityQueueSenderFn
    {
        [FunctionName("PriorityQueueSenderFunction")]
        public static async Task Run(
            [TimerTrigger("0,30 * * * * *")] TimerInfo myTimer,
            [ServiceBus("topic_1", Connection = "ServiceBusConnection")] IAsyncCollector<ServiceBusMessage> collector )
        {
            for (int i = 0; i < 10; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var message = new ServiceBusMessage() { MessageId = messageId };
                message.ApplicationProperties["Priority"] = Priority.Low;
                message.Body = BinaryData.FromString($"Low priority message with Id: {messageId}");
                await collector.AddAsync(message);
            }

            for (int i = 0; i < 10; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var message = new ServiceBusMessage() { MessageId = messageId };
                message.ApplicationProperties["Priority"] = Priority.High;
                message.Body = BinaryData.FromString($"High priority message with Id: {messageId}");
                await collector.AddAsync(message);
            }
        }
    }
}

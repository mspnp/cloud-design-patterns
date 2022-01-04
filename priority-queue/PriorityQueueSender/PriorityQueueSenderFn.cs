using System;
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
            [ServiceBus("messages", Connection = "ServiceBusConnection")] IAsyncCollector<ServiceBusMessage> collector )
        {
            for (int i = 0; i < 10; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var lpMessage = new ServiceBusMessage() { MessageId = messageId };
                lpMessage.ApplicationProperties["Priority"] = Priority.Low;
                lpMessage.Body = BinaryData.FromString($"Low priority message with Id: {messageId}");
                await collector.AddAsync(lpMessage);

                messageId = Guid.NewGuid().ToString();
                var hpMessage = new ServiceBusMessage() { MessageId = messageId };
                hpMessage.ApplicationProperties["Priority"] = Priority.High;
                hpMessage.Body = BinaryData.FromString($"High priority message with Id: {messageId}");
                await collector.AddAsync(hpMessage);
            }
        }
    }
}

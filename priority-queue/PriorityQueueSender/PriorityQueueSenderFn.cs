using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace PriorityQueueSender
{
    public class PriorityQueueSenderFn(ILogger<PriorityQueueSenderFn> logger, ServiceBusClient client)
    {
        private readonly ILogger<PriorityQueueSenderFn> _logger = logger;
        private readonly ServiceBusClient _client = client;

        [Function("PriorityQueueSenderFunction")]
        public async Task Run([TimerTrigger("0,30 * * * * *")] TimerInfo myTimer)
        {
            var sender = _client.CreateSender("messages");
            for (int i = 0; i < 10; i++)
            {
                var messageId = Guid.NewGuid().ToString();
                var lpMessage = new ServiceBusMessage() { MessageId = messageId };
                lpMessage.ApplicationProperties["Priority"] = Priority.Low;
                lpMessage.Body = BinaryData.FromString($"Low priority message with Id: {messageId}");
                await sender.SendMessageAsync(lpMessage);

                messageId = Guid.NewGuid().ToString();
                var hpMessage = new ServiceBusMessage() { MessageId = messageId };
                hpMessage.ApplicationProperties["Priority"] = Priority.High;
                hpMessage.Body = BinaryData.FromString($"High priority message with Id: {messageId}");
                await sender.SendMessageAsync(hpMessage);
            }
        }
    }
}
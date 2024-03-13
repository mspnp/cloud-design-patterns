using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace PriorityQueueConsumerHigh
{
    public class PriorityQueueConsumerHighFn
    {
        private readonly ILogger _logger;

        public PriorityQueueConsumerHighFn(ILogger<PriorityQueueConsumerHighFn> logger)
        {
            _logger = logger;
        }

        [Function("HighPriorityQueueConsumerFunction")]
        public void Run([ServiceBusTrigger("messages", "highPriority", Connection = "ServiceBusConnectionString")] string highPriorityMessage)
        {
            _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {highPriorityMessage}");
        }
    }
}
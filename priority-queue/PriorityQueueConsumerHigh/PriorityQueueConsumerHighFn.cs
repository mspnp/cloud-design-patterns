using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace PriorityQueueConsumerHigh
{
    public class PriorityQueueConsumerHighFn(ILogger<PriorityQueueConsumerHighFn> logger)
    {
        private readonly ILogger _logger = logger;

        [Function("HighPriorityQueueConsumerFunction")]
        public void Run([ServiceBusTrigger("messages", "highPriority", Connection = "ServiceBusConnection")] string highPriorityMessage)
        {
            _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {highPriorityMessage}");
        }
    }
}
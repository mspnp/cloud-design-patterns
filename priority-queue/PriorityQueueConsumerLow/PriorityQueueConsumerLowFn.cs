using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace PriorityQueueConsumerLow
{
    public class PriorityQueueConsumerLowFn(ILogger<PriorityQueueConsumerLowFn> logger)
    {
        private readonly ILogger _logger = logger;

        [Function("LowPriorityQueueConsumerFunction")]
        public void Run([ServiceBusTrigger("messages", "lowPriority", Connection = "ServiceBusConnection")] string lowPriorityMessage)
        {
            _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {lowPriorityMessage}");
        }
    }
}
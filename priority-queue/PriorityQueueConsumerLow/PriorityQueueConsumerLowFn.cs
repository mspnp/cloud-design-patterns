using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

//<docsnippet_namespace_PriorityQueueConsumerLow>
namespace PriorityQueueConsumerLow
{
    public class PriorityQueueConsumerLowFn
    {
        private readonly ILogger _logger;

        public PriorityQueueConsumerLowFn(ILogger<PriorityQueueConsumerLowFn> logger)
        {
            _logger = logger;
        }

        [Function("LowPriorityQueueConsumerFunction")]
        public void Run([ServiceBusTrigger("messages", "lowPriority", Connection = "ServiceBusConnectionString")] string lowPriorityMessage)
        {
            _logger.LogInformation($"C# ServiceBus topic trigger function processed message: {lowPriorityMessage}");
        }
    }
}
//</docsnippet_namespace_PriorityQueueConsumerLow>
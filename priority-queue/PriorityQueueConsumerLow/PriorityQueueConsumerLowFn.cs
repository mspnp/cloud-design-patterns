using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PriorityQueueConsumerLow
{
    public static class PriorityQueueConsumerLowFn
    {
        [FunctionName("LowPriorityQueueConsumerFunction")]
        public static void Run([ServiceBusTrigger("messages", "lowPriority", Connection = "ServiceBusConnection")]string lowPriorityMessage, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {lowPriorityMessage}");
        }
    }
}

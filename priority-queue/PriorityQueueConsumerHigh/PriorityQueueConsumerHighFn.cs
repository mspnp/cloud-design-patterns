using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PriorityQueueConsumerHigh
{
    public static class PriorityQueueConsumerHighFn
    {
        [FunctionName("HighPriorityQueueConsumerFunction")]
        public static void Run([ServiceBusTrigger("messages", "highPriority", Connection = "ServiceBusConnection")]string highPriorityMessage, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {highPriorityMessage}");
        }
    }
}

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PriorityQueueConsumerHigh
{
    public static class PriorityQueueConsumerHighFn
    {
        [FunctionName("HighPriorityQueueConsumerFunction")]
        public static void Run([ServiceBusTrigger("topic_1", "HighPrioritySubscription", Connection = "ServiceBusConnectionString")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
        }
    }
}

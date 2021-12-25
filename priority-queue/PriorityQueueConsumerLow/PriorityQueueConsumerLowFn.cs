using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace PriorityQueueConsumerLow
{
    public static class PriorityQueueConsumerLowFn
    {
        [FunctionName("LowPriorityQueueConsumerFunction")]
        public static void Run([ServiceBusTrigger("topic_1", "LowPrioritySubscription", Connection = "sbConnection")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
        }
    }
}

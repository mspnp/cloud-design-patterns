using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// Sample function illustrating the processing of Service Bus messages containing claim-check style events forwarded by Event Grid
    /// </summary>
    public class FunctionConsumer3(ILoggerFactory loggerFactory, ISampleBlobDataMover sampleBlobDataMover)
    {
        readonly ILogger _logger = loggerFactory.CreateLogger<FunctionConsumer3>();
        readonly ISampleBlobDataMover _downloader = sampleBlobDataMover;

        /// <summary>
        /// Function that processes Service Bus events auto-generated using Event Grid when files are uploaded to a storage blob container.
        /// </summary>
        [Function(nameof(FunctionConsumer3))]
        public async Task RunAsync(
            [ServiceBusTrigger("%ServiceBusQueue%", Connection = "ServiceBusConnection", IsBatched = false, AutoCompleteMessages = true)]
            ServiceBusReceivedMessage receivedMessage
        )
        {
            _logger.LogInformation("Service Bus message received: {MessageId}", receivedMessage.MessageId);

            var messageText = Encoding.UTF8.GetString(receivedMessage.Body);
            var jsonMessage = JsonDocument.Parse(messageText).RootElement;
            var payloadUri = new Uri(jsonMessage.GetProperty("data").GetProperty("url").GetString()!);
            _logger.LogInformation("Message received. Payload Url: {Uri}", payloadUri);

            // download the payload
            var payload = await _downloader.DownloadAsync(payloadUri);
            _logger.LogInformation("Payload content\n {Payload}", payload);
        }
    }
}

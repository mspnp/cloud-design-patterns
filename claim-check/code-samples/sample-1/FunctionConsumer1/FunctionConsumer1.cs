
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// Sample function illustrating the processing of Storage Queue messages containing claim-check style events forwarded by Event Grid
    /// </summary>
    public class FunctionConsumer1(ILoggerFactory loggerFactory, ISampleBlobDataMover sampleBlobDataMover)
    {
        readonly ILogger _logger = loggerFactory.CreateLogger<FunctionConsumer1>();
        readonly ISampleBlobDataMover _downloader = sampleBlobDataMover;

        /// <summary>
        /// Function that processes Storage Queue messages auto-generated by Event Grid when files are uploaded to a storage blob container.
        /// </summary>
        [Function(nameof(FunctionConsumer1))]
        public async Task RunAsync(
            [QueueTrigger("%StorageQueue%", Connection = "StorageQueueConnectionString")] QueueMessage receivedMessage
        )
        {
            _logger.LogInformation("Message Queue message received: {MessageId}", receivedMessage.MessageId);

            var messageText = Encoding.UTF8.GetString(receivedMessage.Body);
            var jsonMessage = JsonDocument.Parse(messageText).RootElement;
            var payloadUri = new Uri(jsonMessage.GetProperty("data").GetProperty("url").GetString()!);
            _logger.LogInformation("Message received. Payload Url: {Uri}", payloadUri);

            // download the payload
            var payload = await _downloader.DownloadAsync(payloadUri);
            _logger.LogInformation("Payload content (Url {Uri)}\n {Payload}", payloadUri, payload);
        }
    }
}

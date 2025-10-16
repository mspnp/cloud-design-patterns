using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// Sample function illustrating the processing of Event Hub events containing claim-check style events. 
    /// </summary>
    public class FunctionConsumer4(ILoggerFactory loggerFactory, ISampleBlobDataMover sampleBlobDataMover)
    {
        readonly ILogger _logger = loggerFactory.CreateLogger<FunctionConsumer4>();
        readonly ISampleBlobDataMover _downloader = sampleBlobDataMover;

        [Function(nameof(FunctionConsumer4))]
        public async Task RunAsync(
            [EventHubTrigger("%EventHubName%", Connection = "EventHubConnectionString", IsBatched = false)] EventData receivedMessage
        )
        {
            _logger.LogInformation("EventHub message received: {SequenceNumber}", receivedMessage.SequenceNumber);

            // retrieve the paylod Uri from the message
            var jsonMessage = JsonDocument.Parse(Encoding.UTF8.GetString(receivedMessage.EventBody)).RootElement;
            var payloadUri = new Uri(jsonMessage.GetProperty("payloadUri").GetString()!);
            _logger.LogInformation("Message received. Payload Url: {Uri}", payloadUri);

            // download the payload
            var payload = await _downloader.DownloadAsync(payloadUri);
            _logger.LogInformation("Payload content (Url {Uri)}\n {Payload}", payloadUri, payload);
        }
    }
}

using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// Sample function illustrating the processing of Event Hub events containing claim-check style events forwarded by Event Grid. 
    /// </summary>
    public class FunctionConsumer2(ILoggerFactory loggerFactory)
    {
        readonly ILogger _logger = loggerFactory.CreateLogger<FunctionConsumer2>();
        readonly SampleBlobDataMover _downloader = new(loggerFactory);

        [Function(nameof(FunctionConsumer2))]
        public async Task RunAsync(
            [EventHubTrigger("%EventHubName%", Connection = "EventHubConnectionString", IsBatched = false)] EventData receivedMessage
        )
        {
            _logger.LogInformation("EventHub message received: {SequenceNumber}", receivedMessage.SequenceNumber);

            // retrieve the paylod Uri from the message
            var jsonEvents = JsonDocument.Parse(Encoding.UTF8.GetString(receivedMessage.EventBody)).RootElement;
            foreach (var jsonEvent in jsonEvents.EnumerateArray())
            {
                var payloadUri = new Uri(jsonEvent.GetProperty("data").GetProperty("url").GetString()!);
                _logger.LogInformation("Message received. Payload Url: {Uri}", payloadUri);

                // download the payload
                var payload = await _downloader.DownloadAsync(payloadUri);
                _logger.LogInformation("Payload content (Url {Uri)}\n {Payload}", payloadUri, payload);
            }
        }
    }
}

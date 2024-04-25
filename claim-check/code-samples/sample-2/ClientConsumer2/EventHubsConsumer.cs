using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// Event Hubs specific logic to receive messages forwarded by Event Grid.
    /// </summary>
    class EventHubsConsumer
    {
        readonly ILogger _logger;
        readonly SampleBlobDataMover _downloader;
        readonly EventProcessorClient _processor;

        public EventHubsConsumer(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EventHubsConsumer>();
            _downloader = new SampleBlobDataMover(loggerFactory);

            var _eventHubsFqdn = configuration.GetSection("AppSettings:EventHubsFullyQualifiedNamespace").Value!;
            var _eventHubsName = configuration.GetSection("AppSettings:EventHubName").Value!;
            var _blobServiceUrl = configuration.GetSection("AppSettings:EventProcessorStorageBlobUrl").Value!;
            var _blobContainerName = configuration.GetSection("AppSettings:EventProcessorStorageContainer").Value!;

            var blobServiceClient = new BlobServiceClient(new Uri(_blobServiceUrl), new DefaultAzureCredential());
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);

            _processor = new EventProcessorClient(
                    blobContainerClient,
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    _eventHubsFqdn,
                    _eventHubsName,
                    new DefaultAzureCredential()
            );
        }

        /// <summary>
        /// Initialize the partition with the current time as the starting position.
        /// </summary>
        Task PartitionInitializingHandler(PartitionInitializingEventArgs eventArgs)
        {
            eventArgs.CancellationToken.ThrowIfCancellationRequested();

            try
            {
                eventArgs.DefaultStartingPosition = EventPosition.FromEnqueuedTime(DateTimeOffset.UtcNow);
                _logger.LogInformation("Initialized partition: {PartitionId}", eventArgs.PartitionId);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error was observed while initializing partition: {PartitionId}.  Message: {Message}", eventArgs.PartitionId, ex.Message);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process the event data recived from Event Hub.
        /// </summary>
        /// <param name="eventArgs"></param>

        async Task ProcessEventHandlerAsync(ProcessEventArgs eventArgs)
        {
            eventArgs.CancellationToken.ThrowIfCancellationRequested();
            if (!eventArgs.HasEvent)
            {
                return;
            }

            try
            {
                var messageText = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
                var jsonEvents = JsonDocument.Parse(messageText).RootElement;
                foreach (var jsonEvent in jsonEvents.EnumerateArray())
                {
                    var payloadUri = new Uri(jsonEvent.GetProperty("data").GetProperty("url").GetString()!);
                    _logger.LogInformation("Message received. Payload Url: {Uri}", payloadUri);

                    // download the payload
                    var payload = await _downloader.DownloadAsync(payloadUri);
                    _logger.LogInformation("Payload content\n {Payload}", payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error was observed while processing events. Message: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Invoked when an error is observed while processing events.
        /// </summary>
        /// <param name="eventArgs"></param>     
        Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            eventArgs.CancellationToken.ThrowIfCancellationRequested();

            // As an example, we'll just log the exception
            // For real-world scenarios, you should take action appropriate to your application.
            _logger.LogError("The error handler was invoked during the operation: {Operation}, for Exception: {Message}", eventArgs.Operation ?? "Unknown", eventArgs.Exception.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Start the processing loop 
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("The application will now start to listen for incoming messages.");

            _processor.PartitionInitializingAsync += PartitionInitializingHandler;
            _processor.ProcessEventAsync += ProcessEventHandlerAsync;
            _processor.ProcessErrorAsync += ProcessErrorHandler;
            await _processor.StartProcessingAsync();
        }

        /// <summary>
        /// Stop the processing loop 
        /// </summary>
        public async Task StopAsync()
        {
            await _processor.StopProcessingAsync();
            _processor.PartitionInitializingAsync -= PartitionInitializingHandler;
            _processor.ProcessEventAsync -= ProcessEventHandlerAsync;
            _processor.ProcessErrorAsync -= ProcessErrorHandler;
        }
    }
}
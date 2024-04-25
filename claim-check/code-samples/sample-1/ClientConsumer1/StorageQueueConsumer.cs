using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// Storage Queue specific logic used by the SampleMessageConsumer class to retrieve messages from Storage Queue.
    /// </summary>
    class StorageQueueConsumer
    {
        readonly TimeSpan visibilityTimeout = TimeSpan.FromSeconds(30);

        readonly QueueClient _queueClient;
        readonly ILogger _logger;
        readonly SampleBlobDataMover _downloader;

        /// <summary>
        /// Initialize a new instance of the QueueClient class, cached for the duration of the program execution.
        /// </summary>
        public StorageQueueConsumer(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StorageQueueConsumer>();
            _downloader = new SampleBlobDataMover(loggerFactory);

            _logger.LogInformation("Connecting to Azure Storage Account...");
            var queueUri = new Uri(configuration.GetSection("AppSettings:StorageQueueUri").Value!);
            _queueClient = new QueueClient(queueUri, new DefaultAzureCredential());

            _logger.LogInformation("Connected to storage queue at: {Uri}.", queueUri.AbsoluteUri);
        }

        /// <summary>
        /// Asynchronously receives a batch of messages from storage queue. Invoked from the SampleMessageConsumer message loop.
        /// </summary>        
        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            foreach (var receivedMessage in (await _queueClient.ReceiveMessagesAsync(maxMessages: 10, visibilityTimeout, cancellationToken)).Value)
            {
                try
                {
                    var messageText = Encoding.UTF8.GetString(Convert.FromBase64String(receivedMessage.MessageText));
                    var jsonMessage = JsonDocument.Parse(messageText).RootElement;
                    var payloadUri = new Uri(jsonMessage.GetProperty("data").GetProperty("url").GetString()!);
                    _logger.LogInformation("Message received. Payload Url: {Uri}", payloadUri);

                    // download the payload
                    var payload = await _downloader.DownloadAsync(payloadUri);
                    _logger.LogInformation("Payload content\n {Payload}", payload);
                    await _queueClient.DeleteMessageAsync(receivedMessage.MessageId, receivedMessage.PopReceipt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error was observed while processing messages.Message: {ex.Message}");
                }
            }
        }
    }
}



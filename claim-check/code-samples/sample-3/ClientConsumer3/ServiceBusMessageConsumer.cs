using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// ServiceBus specific logic used by the SampleMessageConsumer to receive messages from a Service Bus queue.
    /// </summary>
    class ServiceBusMessageConsumer
    {
        const int MaxMessagesInSingleBatch = 10;
        readonly TimeSpan maxWaitTime = TimeSpan.FromSeconds(10);

        readonly ServiceBusClient _client;
        readonly ServiceBusReceiver _receiver;
        readonly ILogger _logger;
        readonly SampleBlobDataMover _downloader;

        /// <summary>
        /// Initializes a new instance of the ServiceBusMessageConsumer class, cached for the duration of the program execution.
        /// </summary>
        /// <param name="configuration"></param>
        public ServiceBusMessageConsumer(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ServiceBusMessageConsumer>();
            _downloader = new SampleBlobDataMover(loggerFactory);

            var serviceBusNamespace = configuration.GetSection("AppSettings:ServiceBusNamespace").Value!;
            var queueName = configuration.GetSection("AppSettings:ServiceBusQueue").Value!;

            //initialize Service Bus client and MessageReceiver
            _logger.LogInformation("Connecting to Azure Service Bus namespace...");
            _client = new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential());
            _receiver = _client.CreateReceiver(
                queueName,
                new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete }
            );
            _logger.LogInformation("Connected to Service Bus {Uri}.", serviceBusNamespace);
        }

        /// <summary>
        /// Asynchronously receives a batch of messages from a Service Bus queue. Invoked from the SampleMessageConsumer loop.
        /// </summary>
        /// <param name="cancellationToken"></param>        
        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            foreach (var receivedMessage in await _receiver.ReceiveMessagesAsync(MaxMessagesInSingleBatch, maxWaitTime, cancellationToken))
            {
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
}
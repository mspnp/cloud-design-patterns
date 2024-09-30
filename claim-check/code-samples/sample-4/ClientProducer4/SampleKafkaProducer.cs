using Azure.Identity;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// Implements the Kafka producer to send messages to a Kafka-enabled Event Hub namespace
    /// Uses Azure Entra ID to authenticate.
    /// </summary>
    public class SampleKafkaProducer(IConfiguration configuration) : ISampleKafkaProducer
    {
        readonly string _eventHubEndpoint = configuration.GetSection("AppSettings:eventHubEndpoint").Value!;
        readonly string _kafkaFqdn = configuration.GetSection("AppSettings:eventHubFqdn").Value!;
        readonly string _topic = configuration.GetSection("AppSettings:eventHubName").Value!;

        public async Task SendMessageAsync(string message)
        {
            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = _kafkaFqdn,
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.OAuthBearer,
                    //Debug = "security,broker,protocol", //Uncomment for librdkafka debugging information
                };

                using var producer = new ProducerBuilder<string, string>(config)
                    .SetOAuthBearerTokenRefreshHandler(async (client, cfg) =>
                    {
                        var credential = new DefaultAzureCredential();
                        var accessToken = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext([_eventHubEndpoint]));
                        client.OAuthBearerSetToken(accessToken.Token, accessToken.ExpiresOn.ToUnixTimeMilliseconds(), credential.GetUserPrincipal());
                    })
                    .Build();
                try
                {
                    var deliveryResult = await producer.ProduceAsync(_topic, new Message<string, string> { Value = message });
                    Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!!! Exception Occurred - {0}", e.Message);
            }
        }
    }
}

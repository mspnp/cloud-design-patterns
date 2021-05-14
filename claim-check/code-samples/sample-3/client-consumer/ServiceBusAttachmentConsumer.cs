using System;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ClientConsumer
{
    class ServiceBusAttachmentConsumer : IConsumer
    {
        private ServiceBusReceiver _receiver;

        public void Configure()
        {
            // Getting connection information from app.config
            string serviceBusConnectionString = ConfigurationManager.AppSettings?["SERVICE_BUS_CONNECTION_STRING"];
            string queueName = ConfigurationManager.AppSettings?["QUEUE_NAME"];


            // Creating and registering the receiver using Service Bus Connection String and Queue Name
            var client = new ServiceBusClient(serviceBusConnectionString);
            _receiver = client.CreateReceiver(queueName);
        }

        public async Task ProcessMessages(CancellationToken cancellationToken)
        {
            // msg will contain the original payload
            var msg = await _receiver.ReceiveMessageAsync().ConfigureAwait(false);

            // The message can be downloaded as a file or even processed further as needed 
            if (msg != null)
                Console.WriteLine("Got the message - " + Encoding.UTF8.GetString(msg.Body));

            if (cancellationToken.IsCancellationRequested)
            {
                await _receiver.CloseAsync();
            }
        }
    }
}
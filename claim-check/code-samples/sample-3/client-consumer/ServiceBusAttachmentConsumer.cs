using System;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using ServiceBus.AttachmentPlugin;

namespace ClientConsumer
{
    class ServiceBusAttachmentConsumer : IConsumer
    {
        private MessageReceiver _receiver;

        public void Configure()
        {
            // Getting connection information from app.config
            string storageConnectionString = ConfigurationManager.AppSettings?["STORAGE_CONNECTION_STRING"];
            string serviceBusConnectionString = ConfigurationManager.AppSettings?["SERVICE_BUS_CONNECTION_STRING"];
            string queueName = ConfigurationManager.AppSettings?["QUEUE_NAME"];

            // Creating config for receiving message
            var config = new AzureStorageAttachmentConfiguration(storageConnectionString);

            // Creating and registering the receiver using Service Bus Connection String and Queue Name
            // This can also be done using SAS tokens - Refer to plugin usage here - https://github.com/SeanFeldman/ServiceBus.AttachmentPlugin
            _receiver = new MessageReceiver(serviceBusConnectionString, queueName, ReceiveMode.ReceiveAndDelete);
            _receiver.RegisterAzureStorageAttachmentPlugin(config);
        }

        public async Task ProcessMessages(CancellationToken cancellationToken)
        {
            // msg will contain the original payload
            var msg = await _receiver.ReceiveAsync().ConfigureAwait(false);

            // The message can be downloaded as a file or even processed further as needed 
            if (msg != null)
                Console.WriteLine("Got the message - "+ Encoding.UTF8.GetString(msg.Body));   

            if (cancellationToken.IsCancellationRequested)
            {
                await _receiver.CloseAsync();
            }    
        }
    }
}
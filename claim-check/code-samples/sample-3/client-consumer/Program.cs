using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using ServiceBus.AttachmentPlugin;
using System;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace client_consumer
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Consumer application has begun...");
            try
            {
                Console.WriteLine("Beginning infinite loop to dequeue messages.");
                Console.WriteLine("Hit Ctrl+C to terminate the application.");
                while (true)
                {
                    callReceiveAsync().Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an exception: {ex.ToString()}");
            }
        }

        static private async Task callReceiveAsync()
        {
            // Getting connection information from app.config
            string storageConnectionString = ConfigurationManager.AppSettings?["STORAGE_CONNECTION_STRING"];
            string serviceBusConnectionString = ConfigurationManager.AppSettings?["SERVICE_BUS_CONNECTION_STRING"];
            string queueName = ConfigurationManager.AppSettings?["QUEUE_NAME"];

            // Creating config for receiving message
            var config = new AzureStorageAttachmentConfiguration(storageConnectionString);

            // Creating and registering the receiver using Service Bus Connection String and Queue Name
            // This can also be done using SAS tokens - Refer to plugin usage here - https://github.com/SeanFeldman/ServiceBus.AttachmentPlugin
            var receiver = new MessageReceiver(serviceBusConnectionString, queueName, ReceiveMode.ReceiveAndDelete);
            receiver.RegisterAzureStorageAttachmentPlugin(config);

            // msg will contain the original payload
            var msg = await receiver.ReceiveAsync().ConfigureAwait(false);

            // The message can be downloaded as a file or even processed further as needed 
            if (msg != null)
                Console.WriteLine("Got the message - "+ Encoding.UTF8.GetString(msg.Body));
        }
    }
}

namespace ReceiverApp
{
 using Microsoft.Azure.ServiceBus;
 using System;
 using System.Configuration;
 using System.Text;
 using System.Threading;
 using System.Threading.Tasks;

 class Program
 {
  // Connection String for the namespace can be obtained from the Azure portal under the 
  // 'Shared Access policies' section.
  private static readonly string queueName = ConfigurationManager.AppSettings.Get("QueueName");
  private static readonly string serviceBusConnectionString = ConfigurationManager.AppSettings.Get("ServiceBusConnectionString");
  static IQueueClient queueClient;
  private static bool keepRunning = true;

  static void Main(string[] args)
  {
   AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

   MainAsync().GetAwaiter().GetResult();
  }

  static async Task MainAsync()
  {
   // Register QueueClient's MessageHandler and receive messages in a loop
   RegisterOnMessageHandlerAndReceiveMessages();

   while (keepRunning) ;
  }

  static void RegisterOnMessageHandlerAndReceiveMessages()
  {
   queueClient = new QueueClient(serviceBusConnectionString, queueName);

   Console.WriteLine("Initializing receiver App");

   var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
   {
    MaxConcurrentCalls = 1,
    AutoComplete = false
   };

   queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
  }

  static async Task ProcessMessagesAsync(Message message, CancellationToken token)
  {
   // Process the message
   Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

   if (!token.IsCancellationRequested)
   {
    await queueClient.CompleteAsync(message.SystemProperties.LockToken);
   }

  }

  static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
  {
   Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
   var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
   Console.WriteLine("Exception context for troubleshooting:");
   Console.WriteLine($"- Endpoint: {context.Endpoint}");
   Console.WriteLine($"- Entity Path: {context.EntityPath}");
   Console.WriteLine($"- Executing Action: {context.Action}");
   return Task.CompletedTask;
  }
  static async void CurrentDomain_ProcessExit(object sender, EventArgs e)
  {
   await queueClient.CloseAsync();
   Console.WriteLine("Exiting receiver app");
  }
 }
}
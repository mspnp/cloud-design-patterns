namespace SenderApp
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
  private static bool keepRunning = true;
  private static int msgCounter = 0;
  static IQueueClient queueClient;

  public static async Task Main(string[] args)
  {

   AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

   queueClient = new QueueClient(serviceBusConnectionString, queueName);

   Console.WriteLine("Web Job started sending messages in a loop.");

   while (keepRunning)
   {
    // Send messages.
    await SendMessagesAsync(msgCounter);
    msgCounter++;
   }

   Console.ReadKey();
  }

  static async Task SendMessagesAsync(int msgCounter)
  {

   try
   {

    // Create a new message to send to the queue
    string messageBody = $"Message {msgCounter}";
    var message = new Message(Encoding.UTF8.GetBytes(messageBody));

    // Write the body of the message to the console
    Console.WriteLine($"Sending message: {messageBody}");

    // Send the message to the queue
    await queueClient.SendAsync(message);

    Console.WriteLine("Message successfully sent.");
    Thread.Sleep(10000);
   }
   catch (Exception exception)
   {
    Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
   }

  }
  static async void CurrentDomain_ProcessExit(object sender, EventArgs e)
  {
   await queueClient.CloseAsync();
   Console.WriteLine("Exiting sender app");
  }
 }
}

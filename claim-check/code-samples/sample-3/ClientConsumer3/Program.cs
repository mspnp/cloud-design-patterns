using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pnp.Samples.ClaimCheckPattern;

var configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", false, false).Build();
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());


Console.WriteLine("Begin sample 3 for Claim Check pattern");
Console.WriteLine("Receives claim-check messages from Service Bus, auto-generated with event-grid when a blob is uploaded to Storage Blob");
Console.WriteLine("Initializing...");
Console.WriteLine();

configuration.ThrowIfMissingSettings([
    "AppSettings:ServiceBusNamespace",
    "AppSettings:ServiceBusQueue"
]);

Console.WriteLine("Receiving messages...");
var serviceBusConsumer = new ServiceBusMessageConsumer(configuration, loggerFactory);
var consumer = new SampleMessageConsumer(serviceBusConsumer.ReceiveMessagesAsync, loggerFactory);
var cts = new CancellationTokenSource();
Task<Task> task = consumer.Start(cts.Token);

Console.WriteLine("Press any key to terminate the application...");
Console.ReadKey(true);
cts.Cancel();

Console.WriteLine("Exiting...");
await task;
Console.WriteLine("Done.");
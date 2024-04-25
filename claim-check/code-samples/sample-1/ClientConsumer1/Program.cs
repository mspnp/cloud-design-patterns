using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pnp.Samples.ClaimCheckPattern;

var configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", false, false).Build();

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

Console.WriteLine("Begin sample 1 for Claim Check pattern");
Console.WriteLine("Receives claim-check messages from Storage Queues, auto-generated with event-grid when a blob is uploaded to Storage Blob");
Console.WriteLine("Initializing...");
Console.WriteLine();

configuration.ThrowIfMissingSettings([
    "AppSettings:StorageQueueUri"
]);

Console.WriteLine("Receiving messages...");
var queueConsumer = new StorageQueueConsumer(configuration, loggerFactory);
var consumer = new SampleMessageConsumer(queueConsumer.ReceiveMessagesAsync, loggerFactory);
var cts = new CancellationTokenSource();
Task<Task> task = consumer.Start(cts.Token);

Console.WriteLine("Press any key to terminate the application...");
Console.ReadKey(true);
cts.Cancel();

Console.WriteLine("Exiting...");
await task;
Console.WriteLine("Done.");

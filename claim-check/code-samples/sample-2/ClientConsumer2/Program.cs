using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pnp.Samples.ClaimCheckPattern;

var configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", false, false).Build();

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

Console.WriteLine("Begin sample 2 for Claim Check pattern");
Console.WriteLine("Uses the Event Hubs EventProcessorClient to receive claim-check messages, auto-generated with event-grid when a blob is uploaded to Storage Blob");
Console.WriteLine("Initializing...");
Console.WriteLine();

configuration.ThrowIfMissingSettings([
    "AppSettings:EventHubsFullyQualifiedNamespace",
    "AppSettings:EventHubName",
    "AppSettings:EventProcessorStorageBlobUrl",
    "AppSettings:EventProcessorStorageContainer"
]);

Console.WriteLine("Receiving messages...");
var eventHubsConsumer = new EventHubsConsumer(configuration, loggerFactory);
var cts = new CancellationTokenSource();
var task = eventHubsConsumer.StartAsync(cts.Token);

Console.WriteLine("Press any key to terminate the application...");
Console.ReadKey(true);
cts.Cancel();

Console.WriteLine("Exiting...");
await task;
await eventHubsConsumer.StopAsync();
Console.WriteLine("Done.");


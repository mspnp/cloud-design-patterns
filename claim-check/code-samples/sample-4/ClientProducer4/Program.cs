using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pnp.Samples.ClaimCheckPattern;
using System.Text.Json;

Console.WriteLine("Begin sample 4 for Claim Check pattern");
Console.WriteLine("Uploads a sample file to Storage Blob and sends a claim-check message via Event Hubs using the Kafka Api");
Console.WriteLine("Initializing...");
Console.WriteLine();

var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", false, false).Build();
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

configuration.ThrowIfMissingSettings([
    "AppSettings:eventHubEndpoint",
            "AppSettings:eventHubFqdn",
            "AppSettings:eventHubName",
            "AppSettings:storageBlobUri",
            "AppSettings:payloadContainerName"
    ]);

Console.WriteLine("Uploading sample file to Azure Blob Storage...");
var blobMover = new SampleBlobDataMover(loggerFactory);
var newBlobUri = await blobMover.UploadAsync(
    configuration.GetSection("AppSettings:StorageBlobUri").Value!,
    configuration.GetSection("AppSettings:PayloadContainerName").Value!,
    "Hello, World! This is a sample text file to illustrate using the claim check cloud pattern");
Console.WriteLine();

Console.WriteLine("Sending Claim Check message...");
var kafkaWorker = new SampleKafkaProducer(configuration);

await kafkaWorker.SendMessageAsync(JsonSerializer.Serialize(new { payloadUri = newBlobUri }));
Console.WriteLine();

Console.WriteLine("Done.");
Console.WriteLine("Press any key to exit the sample application.");
Console.ReadKey();

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        var blobServiceClientConnectionString = Environment.GetEnvironmentVariable("StorageConnectionAppSetting");
        services.AddSingleton(new BlobServiceClient(blobServiceClientConnectionString));
        // ServiceBusClient using Managed Identity
        var fullyQualifiedNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        var serviceBusClient = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        services.AddSingleton(serviceBusClient);
    })
    .Build();

host.Run();

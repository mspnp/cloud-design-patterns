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

        // blobServiceClient using Managed Identity
        var storageAccountUriString = Environment.GetEnvironmentVariable("DataStorage__blobServiceUri");
        var storageContainerEndpoint = storageAccountUriString + "/data";

        // Get a credential and create a client object for the blob container.
        BlobContainerClient containerClient = new BlobContainerClient(new Uri(storageContainerEndpoint),new DefaultAzureCredential());
        services.AddSingleton(containerClient);

        // ServiceBusClient using Managed Identity
        var fullyQualifiedNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        var serviceBusClient = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        services.AddSingleton(serviceBusClient);
    })
    .Build();

host.Run();

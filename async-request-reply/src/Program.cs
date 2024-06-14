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
        var serviceBusClientConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionAppSetting");
        services.AddSingleton(new ServiceBusClient(serviceBusClientConnectionString));
    })
    .Build();

host.Run();

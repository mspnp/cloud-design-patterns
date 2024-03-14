using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(appBuilder =>
    {
        appBuilder.ConfigureBlobStorageExtension();
    })
    .Build();

await host.RunAsync();

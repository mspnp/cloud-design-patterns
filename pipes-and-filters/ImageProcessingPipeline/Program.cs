using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddAzureClients(c =>
        {
            c.UseCredential(new DefaultAzureCredential());
            c.AddBlobServiceClient(hostContext.Configuration.GetSection("output")).WithName("processed");
        });
    })
    .Build();

host.Run();

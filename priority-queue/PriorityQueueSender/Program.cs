using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System;
using Microsoft.Extensions.Azure;

var host = new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureAppConfiguration((hostingContext, config) =>
  {
      config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
  })
  .ConfigureServices(services =>
  {
      var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

      services.AddSingleton(configuration);

      services.AddAzureClients(builder =>
      {
          builder.AddServiceBusClientWithNamespace(Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace"))
                 .WithCredential(new DefaultAzureCredential());
      });
  })
  .Build();

host.Run();
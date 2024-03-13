using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Messaging.ServiceBus;

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

      services.AddSingleton<ServiceBusClient>(sp =>
      {
          var connectionString = configuration.GetValue<string>("ServiceBusConnectionString");
          return new ServiceBusClient(connectionString);
      });
  })
  .Build();

host.Run();
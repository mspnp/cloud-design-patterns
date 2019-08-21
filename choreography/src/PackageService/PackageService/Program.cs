using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

namespace PackageService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Fabrikam Package Service is starting.");

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
              WebHost.CreateDefaultBuilder(args)
                   .ConfigureAppConfiguration((hostingContext, config) =>
                   {
                       config.AddEnvironmentVariables();
                   })
                  .UseStartup<Startup>()                
                  .ConfigureLogging((hostingContext, loggingBuilder) =>
                  {
                      loggingBuilder.AddApplicationInsights();
                      loggingBuilder.AddSerilog(dispose: true);
                  })
                  
                  .UseUrls("http://0.0.0.0:8080");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabrikam.DroneDelivery.PackageService.Middlewares.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageService.Services;
using Serilog;
using Serilog.Formatting.Compact;
using Swashbuckle.AspNetCore.Swagger;

namespace PackageService
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
               .AddEnvironmentVariables();

            var buildConfig = builder.Build();

            if (buildConfig["KEY_VAULT_URI"] is var keyVaultUri && !string.IsNullOrEmpty(keyVaultUri))
            {
                builder.AddAzureKeyVault(keyVaultUri);
            }

            // Get CosmosDB database and collection and store it in DocumentConfig. Those values
            // are accessd by the PackageRepository class.
            if (buildConfig["DOCDB_DATABASEID"] is var database && !string.IsNullOrEmpty(database))
            {
                DocumentConfig.DatabaseId = database;
            }
            if (buildConfig["DOCDB_COLLECTIONID"] is var collection && !string.IsNullOrEmpty(collection))
            {
                DocumentConfig.CollectionId = collection;
            }

            Configuration = builder.Build();



        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Configure AppInsights
            services.AddApplicationInsightsKubernetesEnricher();
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton<IPackageRepository, PackageRepository>();
            services.AddSingleton<IDocumentClient>(new DocumentClient(new Uri(Configuration["CosmosDB:Endpoint"]),
                                                        Configuration["CosmosDB:AuthKey"]));

            DocumentConfig.DatabaseId = Configuration["CosmosDB:Database"];
            DocumentConfig.CollectionId = Configuration["CosmosDB:Collection"];

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Fabrikam DroneDelivery PackageService API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console(new CompactJsonFormatter())
                        .ReadFrom.Configuration(Configuration)
                        .CreateLogger();

            // Important: it has to be first: enable global logger
            app.UseGlobalLoggerHandler();

            // Important: it has to be second: Enable global exception, error handling
            app.UseGlobalExceptionHandler();

            // TODO: Add middleware AuthZ here

            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fabrikam DroneDelivery DeliveryService API V1");
            });
        }
    }
}

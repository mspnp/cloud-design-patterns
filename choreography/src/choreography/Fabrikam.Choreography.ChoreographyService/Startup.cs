
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
//


using System;
using Fabrikam.Choreography.ChoreographyService.Services;
using Fabrikam.Communicator.Middlewares.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;
using Swashbuckle.AspNetCore.Swagger;


namespace Fabrikam.Choreography.ChoreographyService
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
            // Get CosmosDB database and collection and store it in DocumentConfig. Those values
            // are accessd by the PackageRepository class.       

            Configuration = builder.Build();


            if (buildConfig["AzureKeyVault:KeyVaultUri"] is var keyVaultUri && !string.IsNullOrEmpty(keyVaultUri))
            {
                builder.AddAzureKeyVault(keyVaultUri);
            }

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IEventRepository>(s =>
              new EventRepository(Configuration.GetValue<string>("ENV_TOPIC_ENDPOINT")
              ,Configuration.GetValue<string>("ENV_TOPICKEY_VALUE")
              , Configuration.GetValue<string>("ENV_TOPICS")));

            // Configure AppInsights
            services.AddApplicationInsightsKubernetesEnricher();
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services
            .AddHttpClient<IPackageServiceCaller, PackageServiceCaller>(c =>
           {
               c.BaseAddress = new Uri(Configuration["SERVICE_URI_PACKAGE"]);
           });

            services
                .AddHttpClient<IDroneSchedulerServiceCaller, DroneSchedulerServiceCaller>(c =>
                {
                    c.BaseAddress = new Uri(Configuration["SERVICE_URI_DRONE"]);
                });

            services
                .AddHttpClient<IDeliveryServiceCaller, DeliveryServiceCaller>(c =>
                {
                    c.BaseAddress = new Uri(Configuration["SERVICE_URI_DELIVERY"]);
                });
                
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

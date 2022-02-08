﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.DroneDelivery.DeliveryService.Middlewares.Builder;
using Fabrikam.DroneDelivery.DeliveryService.Models;
using Fabrikam.DroneDelivery.DeliveryService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace Fabrikam.DroneDelivery.DeliveryService
{
    public class Startup
    {
        private const string HealCheckName = "ReadinessLiveness";

        public Startup(IWebHostEnvironment env)
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

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Configure AppInsights
            services.AddApplicationInsightsKubernetesEnricher();
            services.AddApplicationInsightsTelemetry(Configuration);

            // Add health check
            services.AddHealthChecks().AddCheck(
                    HealCheckName,
                    () => HealthCheckResult.Healthy("OK"));

            services.AddControllers();

            services.AddSingleton<IDeliveryRepository, DeliveryRepository>();
            services.AddSingleton<INotifyMeRequestRepository, NotifyMeRequestRepository>();
            services.AddSingleton<INotificationService, NoOpNotificationService>();
            services.AddSingleton<IDeliveryTrackingEventRepository, DeliveryTrackingRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
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

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapControllers();
            });

            //TODO look into creating a factory of DocDBRepos/RedisCache/EventHubMessenger
            DocumentDBRepository<InternalNotifyMeRequest>.Configure(Configuration["CosmosDB-Endpoint"], Configuration["CosmosDB-Key"], Configuration["DOCDB_DATABASEID"], Configuration["DOCDB_COLLECTIONID"], loggerFactory);
            RedisCache<InternalDelivery>.Configure(Constants.RedisCacheDBId_Delivery, Configuration["Redis-Endpoint"], Configuration["Redis-AccessKey"], loggerFactory);
            RedisCache<DeliveryTrackingEvent>.Configure(Constants.RedisCacheDBId_DeliveryStatus, Configuration["Redis-Endpoint"], Configuration["Redis-AccessKey"], loggerFactory);
        }
    }
}

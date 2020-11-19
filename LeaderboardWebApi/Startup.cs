using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthChecks.UI.Client;
using LeaderboardWebApi.Infrastructure;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Newtonsoft.Json;
using NSwag.AspNetCore;

namespace LeaderboardWebApi
{
    public class Startup
    {
        private readonly IHostEnvironment hostEnvironment;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            this.hostEnvironment = hostEnvironment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<LeaderboardContext>(options =>
            {
                string connectionString =
                    Configuration.GetConnectionString("LeaderboardContext");
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                });
            });

            ConfigureFeatures(services);
            ConfigureApiOptions(services);
            ConfigureTelemetry(services);
            ConfigureOpenApi(services);
            ConfigureSecurity(services);
            ConfigureHealth(services);
            ConfigureVersioning(services);

            services.AddControllers(options => {
                    //options.Filters.AddForFeature<ThirdPartyActionFilter>(nameof(ApiFeatureFlags.EnhancedPipeline))
                })
                .AddNewtonsoftJson(setup => {
                    setup.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .AddControllersAsServices(); // For resolving controllers as services via DI
        }

        private void ConfigureFeatures(IServiceCollection services)
        {
            services.AddFeatureManagement();
        }

        private void ConfigureVersioning(IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });
        }

        private void ConfigureHealth(IServiceCollection services)
        {
            IHealthChecksBuilder builder = services.AddHealthChecks();
            string key = Configuration["ApplicationInsights:InstrumentationKey"];
            
            builder.AddApplicationInsightsPublisher(key);
            services.Configure<HealthCheckPublisherOptions>(options => {
                options.Delay = TimeSpan.FromSeconds(10);
            });
        }

        private void ConfigureTelemetry(IServiceCollection services)
        {
            services.AddSingleton<ITelemetryInitializer, ServiceNameInitializer>();
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.DeveloperMode = hostEnvironment.IsDevelopment();
                options.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
            });

            var performanceCounterService = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(PerformanceCollectorModule));
            if (performanceCounterService != null)
            {
                services.Remove(performanceCounterService);
            }
        }

        private void ConfigureSecurity(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });
        }

        private void ConfigureOpenApi(IServiceCollection services)
        {
            services.AddOpenApiDocument(document =>
            {
                document.DocumentName = "v1";
                document.PostProcess = d => d.Info.Title = "Retro Gaming 2019 OpenAPI";
            });
        }

        private void ConfigureApiOptions(IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Instance = context.HttpContext.Request.Path,
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://asp.net/core",
                        Detail = "Please refer to the errors property for additional details."
                    };
                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json", "application/problem+xml" }
                    };
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHealthChecks("/health");
            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            // Use Azure App Configuration to allow requests to trigger refresh of the configuration
            app.UseAzureAppConfiguration();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/ready",
                    new HealthCheckOptions()
                    {
                        Predicate = reg => reg.Tags.Contains("ready"),
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    })
                .RequireHost($"*:{Configuration["ManagementPort"]}");

                endpoints.MapHealthChecks("/health/lively",
                    new HealthCheckOptions()
                    {
                        Predicate = _ => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    })
                .RequireHost($"*:{Configuration["ManagementPort"]}");
                endpoints.MapControllers();
            });
        }

        public void ConfigureDevelopment(IApplicationBuilder app, IWebHostEnvironment env, 
            LeaderboardContext context, TelemetryConfiguration configuration)
        {
            // configuration.DisableTelemetry = true;

            DbInitializer.Initialize(context).Wait();
            app.UseDeveloperExceptionPage();

            app.UseOpenApi(config =>
            {
                config.DocumentName = "v1";
                config.Path = "/openapi/v1.json";
            });
            app.UseSwaggerUi3(config =>
            {
                config.SwaggerRoutes.Add(new SwaggerUi3Route("v1.0", "/openapi/v1.json"));

                config.Path = "/openapi";
                config.DocumentPath = "/openapi/v1.json";
            });

            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseAzureAppConfiguration();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/ping", new HealthCheckOptions() { Predicate = _ => false });

                HealthCheckOptions options = new HealthCheckOptions();
                options.ResultStatusCodes[HealthStatus.Degraded] = 418; // I'm a tea pot (or other HttpStatusCode enum)
                options.AllowCachingResponses = true;
                options.Predicate = _ => true;
                options.ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse;
                endpoints.MapHealthChecks("/health", options);

                endpoints.MapHealthChecksUI();
                endpoints.MapControllers();
            });
        }
    }
}
using System;
using System.Net.Http;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Polly;
using Refit;
using RetroGamingWebApp.Proxy;

namespace RetroGamingWebApp
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
            services.Configure<LeaderboardApiOptions>(Configuration.GetSection("LeaderboardApiOptions"));
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            ConfigureFeatures(services);
            ConfigureTypedClients(services);
            ConfigureSecurity(services);
            ConfigureTelemetry(services);

            services.Configure<Settings>(Configuration.GetSection("AppConfiguration:Settings"));

            services.AddRazorPages();
        }

        private void ConfigureFeatures(IServiceCollection services)
        {
            services.AddFeatureManagement()
                .AddFeatureFilter<PercentageFilter>()
                .UseDisabledFeaturesHandler(new FeatureNotEnabledDisabledHandler());
        }


        private void ConfigureTelemetry(IServiceCollection services)
        {
            services.AddSingleton<ITelemetryInitializer, ServiceNameInitializer>();            
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.DeveloperMode = hostEnvironment.IsDevelopment();
                options.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
            });
        }

        private void ConfigureSecurity(IServiceCollection services)
        {
            services.AddHsts(
                options =>
                {
                    options.MaxAge = TimeSpan.FromDays(100);
                    options.IncludeSubDomains = true;
                    options.Preload = true;
                });
        }

        private void ConfigureTypedClients(IServiceCollection services)
        {
            services.AddHttpClient("WebAPIs", options =>
            {
                options.BaseAddress = new Uri(Configuration["LeaderboardApiOptions:BaseUrl"]);
                options.Timeout = TimeSpan.FromMilliseconds(15000);
                options.DefaultRequestHeaders.Add("ClientFactory", "Check");
            })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500)))
            .AddTransientHttpErrorPolicy(p => p.RetryAsync(3))
            .AddTypedClient(client => RestService.For<ILeaderboardClient>(client));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();

            // Use Azure App Configuration to allow requests to trigger refresh of the configuration
            app.UseAzureAppConfiguration();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
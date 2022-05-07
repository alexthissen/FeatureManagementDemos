using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Refit;
using RetroGamingWebApp;
using RetroGamingWebApp.Extensions;
using RetroGamingWebApp.Proxy;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

string connection = builder.Configuration.GetConnectionString("AppConfig");
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.ConfigureRefresh(refresh =>
    {
        refresh.Register("LeaderboardWebApp:Settings:Sentinel", refreshAll: true)
            .SetCacheExpiration(new TimeSpan(0, 1, 0));
    });
    options.Connect(connection);
    options.Select(KeyFilter.Any);
    options.UseFeatureFlags(feature =>
    {
        feature.CacheExpirationInterval = TimeSpan.FromSeconds(30);
        feature.Label = builder.Environment.EnvironmentName;
    });
});
// Required for refresh
builder.Services.AddAzureAppConfiguration();

builder.Services.Configure<LeaderboardApiOptions>(builder.Configuration.GetSection("LeaderboardApiOptions"));
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

builder.AddMonitoring();
builder.Services.Configure<Settings>(builder.Configuration.GetSection("LeaderboardWebApp:Settings"));
builder.Services.AddRazorPages();

builder.Services.AddFeatureManagement()
    .AddFeatureFilter<PercentageFilter>()
    .UseDisabledFeaturesHandler(new FeatureNotEnabledDisabledHandler());
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
//builder.Services.AddTransient<ISessionManager, FeatureSessionManager>();

builder.Services.AddHsts(
    options =>
    {
        options.MaxAge = TimeSpan.FromDays(100);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });

var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));
var retry = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TimeoutRejectedException>()
    .RetryAsync(3, onRetry: (exception, retryCount) => {
        Trace.TraceInformation($"Retry #{retryCount}");
    });

builder.Services.AddHttpClient("WebAPIs", options =>
{
    options.BaseAddress = new Uri(builder.Configuration["LeaderboardApiOptions:BaseUrl"]);
    options.Timeout = TimeSpan.FromMilliseconds(15000);
    options.DefaultRequestHeaders.Add("ClientFactory", "Check");
})
.AddPolicyHandler(retry.WrapAsync(timeout))
.AddTypedClient(client => RestService.For<ILeaderboardClient>(client));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Allow requests to trigger refresh of configuration
app.UseAzureAppConfiguration();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.UseSession();
app.MapRazorPages();

app.Run();

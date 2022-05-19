using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using RetroGamingWebApp;
using RetroGamingWebApp.Instrumentation;
using RetroGamingWebApp.Proxy;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

string connection = builder.Configuration.GetConnectionString("AppConfig");
if (!String.IsNullOrEmpty(connection))
{
    builder.Configuration.AddAzureAppConfiguration(options => {
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
    },
    // Important to set to true for missing App Configuration service
    optional: !builder.Environment.IsDevelopment()); 
    
    // Required for refresh
    builder.Services.AddAzureAppConfiguration();
}

builder.Services.Configure<LeaderboardApiOptions>(builder.Configuration.GetSection("LeaderboardApiOptions"));
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// Custom extensions
builder.AddInstrumentation();
builder.AddClientProxy();

builder.Services.Configure<Settings>(builder.Configuration.GetSection("LeaderboardWebApp:Settings"));
builder.Services.AddRazorPages();

// Feature management
builder.Services.AddFeatureManagement()
    .AddFeatureFilter<PercentageFilter>()
    .UseDisabledFeaturesHandler(new RetroGamingWebAppDisabledFeaturesHandler());

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
// Uncomment next line to have features per session
//builder.Services.AddTransient<ISessionManager, FeatureSessionManager>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Allow requests to trigger refresh of configuration
if (!String.IsNullOrEmpty(connection)) app.UseAzureAppConfiguration();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.UseSession();

app.MapRazorPages();

app.Run();

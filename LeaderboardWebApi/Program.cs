using LeaderboardWebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

string[] arguments = new[] {
    "--environment=Development",
    "--contentRoot=C:\\Sources\\GitHub\\alexthissen\\FeatureManagement\\LeaderboardWebApi",
    "--applicationName=LeaderboardWebApi"
};

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LeaderboardContext>(options =>
{
    string connectionString =
        builder.Configuration.GetConnectionString("LeaderboardContext");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null);
    });
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllers(options => {
    //options.Filters.AddForFeature<ThirdPartyActionFilter>(nameof(ApiFeatureFlags.EnhancedPipeline))
})
.AddNewtonsoftJson(setup => {
    setup.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
})
.AddControllersAsServices(); // For resolving controllers as services via DI

string connection = builder.Configuration.GetConnectionString("AppConfig");
if (!String.IsNullOrEmpty(connection))
{
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
    }, true); // Important to set for missing App Configuration service

    // Required for refresh
    builder.Services.AddAzureAppConfiguration();
}

builder.Services.AddFeatureManagement()
    .AddFeatureFilter<PercentageFilter>()
    .AddFeatureFilter<ProxyVersionFilter>(); // Custom filter

builder.AddInstrumentation();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Retro Videogames Leaderboard WebAPI", Version = "v1.0" });
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
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

WebApplication app = builder.Build();

app.MapInstrumentation();

if (app.Environment.IsDevelopment())
{
    // ApplicationServices does not exist anymore
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<LeaderboardContext>().Database.EnsureCreated();
    }
    app.UseDeveloperExceptionPage();
    app.UseSwagger(options => {
        options.RouteTemplate = "openapi/{documentName}/openapi.json";
    });
    app.UseSwaggerUI(c =>{
        c.SwaggerEndpoint("/openapi/v1.0/openapi.json", "LeaderboardWebAPI v1.0");
        c.RoutePrefix = "openapi";
    });
}

// Use Azure App Configuration to allow requests to trigger refresh of the configuration
if (!String.IsNullOrEmpty(connection)) app.UseAzureAppConfiguration();

app.UseAuthorization();
app.MapControllers();
app.Run();
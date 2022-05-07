using Microsoft.ApplicationInsights.Extensibility;

namespace RetroGamingWebApp.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddMonitoring(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<ITelemetryInitializer, ServiceNameInitializer>();
            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.DeveloperMode = builder.Environment.IsDevelopment();
                options.InstrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];
            });
            return builder;
        }
    }
}

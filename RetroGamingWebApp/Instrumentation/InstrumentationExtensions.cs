using Microsoft.ApplicationInsights.Extensibility;

namespace RetroGamingWebApp.Instrumentation
{
    public static class InstrumentationExtensions
    {
        public static WebApplicationBuilder AddInstrumentation(this WebApplicationBuilder builder)
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

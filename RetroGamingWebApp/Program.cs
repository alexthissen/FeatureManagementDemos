using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RetroGamingWebApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    var configuration = builder.Build();

                    if (!string.IsNullOrEmpty(configuration["ConnectionStrings:AppConfig"]))
                    {
                        builder.AddAzureAppConfiguration(options =>
                        {
                            options.Connect(configuration["ConnectionStrings:AppConfig"]);
                            options.Use(KeyFilter.Any);
                            options.UseFeatureFlags();
                            //options.UseFeatureFlags(options => { 
                            //    options.Label = ctx.HostingEnvironment.EnvironmentName; 
                            //});
                        });
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

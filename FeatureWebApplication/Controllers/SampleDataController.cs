using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace FeatureWebApplication.Controllers
{
    [Route("api/[controller]")]
    public class SampleDataController : Controller
    {
        private readonly IFeatureManager _featureManager;
        private readonly IConfiguration configuration;
        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        public SampleDataController(IFeatureManagerSnapshot featureManager, IConfiguration configuration)
        {   
            _featureManager = featureManager;
            this.configuration = configuration;
        }

        [HttpGet("[action]")]
        [Feature("Beta")]
        public IEnumerable<FeatureValue> Features()
        {
            var section = configuration.GetSection("FeatureManagement");
            return null;
        }


        public class FeatureValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class WeatherForecast
        {
            public string DateFormatted { get; set; }
            public int TemperatureC { get; set; }
            public string Summary { get; set; }

            public int TemperatureF
            {
                get
                {
                    return 32 + (int)(TemperatureC / 0.5556);
                }
            }
        }
    }
}

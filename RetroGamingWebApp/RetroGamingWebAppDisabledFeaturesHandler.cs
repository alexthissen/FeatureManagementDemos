using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetroGamingWebApp
{
    public class RetroGamingWebAppDisabledFeaturesHandler : IDisabledFeaturesHandler
    {
        public Task HandleDisabledFeatures(IEnumerable<string> features, ActionExecutingContext context)
        {
            context.Result = new ContentResult
            {
                ContentType = "text/plain",
                Content = "Unavailable feature. Please try again later - " + String.Join(',', features),
                StatusCode = StatusCodes.Status404NotFound
            };
            return Task.CompletedTask;
        }
    }
}
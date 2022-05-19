using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Refit;
using System.Diagnostics;

namespace RetroGamingWebApp.Proxy
{
    public static class ClientProxyExtensions
    {
        public static void AddClientProxy(this WebApplicationBuilder builder)
        {
            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));
            var retry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .RetryAsync(3, onRetry: (exception, retryCount) =>
                {
                    Trace.TraceInformation($"Retry #{retryCount}");
                });

            builder.Services.AddHttpClient("WebAPIs", options =>
            {
                options.BaseAddress = new Uri(builder.Configuration["LeaderboardApiOptions:BaseUrl"]);
                options.Timeout = TimeSpan.FromMilliseconds(15000);
                options.DefaultRequestHeaders.Add("ClientFactory", "true");
            })
            .AddPolicyHandler(retry.WrapAsync(timeout))
            .AddTypedClient(client => RestService.For<ILeaderboardClient>(client));

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Polly.Timeout;
using RetroGamingWebApp.Proxy;

namespace RetroGamingWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private readonly IOptionsSnapshot<LeaderboardApiOptions> options;
        private readonly IOptionsSnapshot<Settings> settings;
        private readonly IFeatureManagerSnapshot featureManager;
        private readonly ILeaderboardClient proxy;

        public IndexModel(ILoggerFactory loggerFactory, ILeaderboardClient proxy, 
            IOptionsSnapshot<LeaderboardApiOptions> options, 
            IOptionsSnapshot<Settings> settings,
            IFeatureManagerSnapshot featureManager)
        {
            this.logger = loggerFactory.CreateLogger<IndexModel>();
            this.options = options;
            this.settings = settings;
            this.featureManager = featureManager;
            this.proxy = proxy;
        }

        public IEnumerable<HighScore> Scores { get; private set; }

        public async Task OnGetAsync()
        {
            Scores = new List<HighScore>();
            try
            {
                //ILeaderboardClient proxy = RestService.For<ILeaderboardClient>(options.Value.BaseUrl);
                if (await featureManager.IsEnabledAsync(nameof(AppFeatureFlags.LeaderboardListLimit)))
                {
                    int limit;
                    Scores = await proxy.GetHighScores(Int32.TryParse(Request.Query["limit"], out limit) ? limit : 5)
                        .ConfigureAwait(false);
                }
                else
                {
                    Scores = await proxy.GetHighScores().ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Http request failed.");
            }
            catch (TimeoutRejectedException ex)
            {
                logger.LogWarning(ex, "Timeout occurred when retrieving high score list.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unknown exception occurred while retrieving high score list");
                throw;
            }
        }
    }
}

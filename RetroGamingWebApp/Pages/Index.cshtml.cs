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
using RetroGamingWebApp.Features;
using RetroGamingWebApp.Proxy;

namespace RetroGamingWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private const int DefaultLeaderboardSize = 10;

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
        public int LeaderboardSize { get; private set; }

        public async Task OnGetAsync()
        {
            Scores = new List<HighScore>();
            try
            {
                if (await featureManager.IsEnabledAsync(nameof(AppFeatureFlags.LeaderboardSize)))
                {
                    LeaderboardSize = Int32.TryParse(Request.Query["size"], out var size) && size < 20
                        ? size : DefaultLeaderboardSize;
                    Scores = await proxy.GetHighScores(LeaderboardSize)
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

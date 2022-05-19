using LeaderboardWebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace LeaderboardWebApi.Controllers
{
    public record struct HighScore
    {
        public string Game { get; set; }
        public string Nickname { get; set; }
        public int Points { get; set; }
    }

    [ApiController]
    [Route("api/v1.0/[controller]")]
    [Produces("application/xml", "application/json")]
    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardContext context;
        private readonly IFeatureManager featureManager;
        private readonly IHostEnvironment environment;
        private readonly ILogger<LeaderboardController> logger;

        public LeaderboardController(LeaderboardContext context, IFeatureManager featureManager, IConfiguration config,
            IHostEnvironment environment,
            ILoggerFactory loggerFactory = null)
        {
            this.context = context;
            this.featureManager = featureManager;
            this.environment = environment;
            this.logger = loggerFactory?.CreateLogger<LeaderboardController>();
        }

        // GET api/leaderboard
        /// <summary>
        /// Retrieve a list of leaderboard scores.
        /// </summary>
        /// <returns>List of high scores per game.</returns>
        /// <response code="200">The list was successfully retrieved.</response>
        [HttpGet(Name = "GetLeaderboard")]
        [ProducesResponseType(typeof(IEnumerable<HighScore>), 200)]
        public async Task<ActionResult<IEnumerable<HighScore>>> Get(int limit = 0)
        {
            logger?.LogInformation("Retrieving score list with a limit of {SearchLimit}.", limit);

            var scores = context.Scores
                .Select(score => new HighScore()
                {
                    Game = score.Game,
                    Points = score.Points,
                    Nickname = score.Gamer.Nickname
                });

            // Evaluate feature flag
            if (await featureManager.IsEnabledAsync(nameof(ApiFeatureFlags.LeaderboardListLimit)))
            {
                // New functionality inside if statement
                int searchLimit = limit;

                // This is a demo bug, supposedly "hard" to spot
                do
                {
                    searchLimit--;
                }
                while (searchLimit != 0);

                scores = scores.Take(limit);
            }

            return Ok(await scores.ToListAsync().ConfigureAwait(false));
        }

        [HttpGet("/[action]")]
        [FeatureGate("Alpha")]
        public async Task<IAsyncEnumerable<string>> Features() => featureManager.GetFeatureNamesAsync();

        // Require both feature flags to be enabled
        [FeatureGate(RequirementType.All, "Beta", "Alpha")]
        [HttpGet("/[action]")]
        public async Task<Dictionary<string, bool>> FeaturesWithValues()
        {
            IAsyncEnumerable<string> names = featureManager.GetFeatureNamesAsync();
            var results = new Dictionary<string, bool>();
            await foreach (var name in names
                .WithCancellation(default(CancellationToken))
                .ConfigureAwait(false))
            {
                results.Add(name, await featureManager.IsEnabledAsync(name));
            }
            return results;
        }
    }
}

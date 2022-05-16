using Microsoft.FeatureManagement;

namespace LeaderboardWebApi.Infrastructure
{
    [FilterAlias("Xpirit.ProxyVersion")]
    public class ProxyVersionFilter : IFeatureFilter
    {
        private const string ProxyVersionParameter = "ProxyVersion";
        IHttpContextAccessor httpContextAccessor;

        public ProxyVersionFilter(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext evaluation)
        {
            HttpContext context = httpContextAccessor.HttpContext;
            string version = context.Request.Headers["Proxy-Version"];
            if (string.IsNullOrEmpty(version)) 
                return Task.FromResult(false);

            return Task.FromResult(
                String.Equals(evaluation.Parameters[ProxyVersionParameter],
                version, StringComparison.OrdinalIgnoreCase));
        }
    }
}

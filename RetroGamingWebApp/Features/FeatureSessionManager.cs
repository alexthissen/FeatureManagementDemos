using Microsoft.FeatureManagement;

namespace RetroGamingWebApp.Features
{
    public class FeatureSessionManager : ISessionManager
    {
        private readonly IHttpContextAccessor contextAccessor;

        public FeatureSessionManager(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        public async Task<bool?> GetAsync(string featureName)
        {
            var session = contextAccessor.HttpContext.Session;
            var sessionKey = $"feature_{featureName}";
            if (session.TryGetValue(sessionKey, out var enabledBytes))
            {
                return enabledBytes[0] == 1;
            }

            return null;
        }

        public async Task SetAsync(string featureName, bool enabled)
        {
            var session = contextAccessor.HttpContext.Session;
            var sessionKey = $"feature_{featureName}";
            session.Set(sessionKey, new byte[] { enabled ? (byte)1 : (byte)0 });
        }
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PlayHT.Services
{
    public class PlayHtHealthCheck : IHealthCheck
    {
        private readonly IPlayHtService _playHtService;

        public PlayHtHealthCheck(IPlayHtService playHtService)
        {
            _playHtService = playHtService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _playHtService.GetVoicesAsync();
                return HealthCheckResult.Healthy("PlayHT API is responsive");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("PlayHT API is not responding", ex);
            }
        }
    }
}

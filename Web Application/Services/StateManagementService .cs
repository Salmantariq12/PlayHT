using Microsoft.Extensions.Caching.Distributed;
using PlayHT.Models;
using System.Text.Json;

namespace PlayHT.Services
{
    public class StateManagementService : IStateManagementService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<StateManagementService> _logger;

        public StateManagementService(IDistributedCache cache, ILogger<StateManagementService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task SaveDraftAsync(string userId, VoiceAgent agent)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                };

                var serializedAgent = JsonSerializer.Serialize(agent);
                await _cache.SetStringAsync($"draft_{userId}", serializedAgent, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving draft for user {UserId}", userId);
                throw;
            }
        }

        public async Task<VoiceAgent> LoadDraftAsync(string userId)
        {
            try
            {
                var serializedAgent = await _cache.GetStringAsync($"draft_{userId}");
                return serializedAgent == null ? null : JsonSerializer.Deserialize<VoiceAgent>(serializedAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading draft for user {UserId}", userId);
                throw;
            }
        }

        public async Task DeleteDraftAsync(string userId)
        {
            await _cache.RemoveAsync($"draft_{userId}");
        }
    }
}

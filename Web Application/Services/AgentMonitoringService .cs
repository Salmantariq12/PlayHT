using PlayHT.Models;
using PlayHT.Models.Enums;

namespace PlayHT.Services
{
    public class AgentMonitoringService : IAgentMonitoringService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentMonitoringService> _logger;

        public AgentMonitoringService(HttpClient httpClient, ILogger<AgentMonitoringService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AgentStatus> GetAgentStatusAsync(string agentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.play.ai/api/v1/agents/{agentId}/status");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<AgentStatus>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for agent {AgentId}", agentId);
                throw;
            }
        }

        public async Task<List<AgentInteraction>> GetRecentInteractionsAsync(string agentId)
        {
            var response = await _httpClient.GetAsync($"https://api.play.ai/api/v1/agents/{agentId}/interactions");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<AgentInteraction>>();
        }

        public async Task<AgentMetrics> GetAgentMetricsAsync(string agentId)
        {
            var response = await _httpClient.GetAsync($"https://api.play.ai/api/v1/agents/{agentId}/metrics");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AgentMetrics>();
        }
    }
}

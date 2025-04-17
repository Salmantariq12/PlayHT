using PlayHT.Models;
using PlayHT.Models.Enums;

namespace PlayHT.Services
{
    public interface IAgentMonitoringService
    {
        Task<AgentStatus> GetAgentStatusAsync(string agentId);
        Task<List<AgentInteraction>> GetRecentInteractionsAsync(string agentId);
        Task<AgentMetrics> GetAgentMetricsAsync(string agentId);
    }

}

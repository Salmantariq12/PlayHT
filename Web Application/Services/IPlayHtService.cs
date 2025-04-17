using PlayHT.Models;
using PlayHT.Models.Enums;

namespace PlayHT.Services
{
    public interface IPlayHtService
    {
        Task<List<Voice>> GetVoicesAsync();
        Task<string> GenerateVoiceSampleAsync(string voiceId, string text);
        Task<string> CreateAgentAsync(VoiceAgent agent);
        Task<byte[]> GetAudioStreamAsync(string audioUrl);
        Task<AgentStatus> GetAgentStatusAsync(string agentId);
        Task<AgentDeploymentStatus> GetAgentDeploymentStatusAsync(string agentId);
    }
}

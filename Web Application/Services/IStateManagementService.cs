using PlayHT.Models;

namespace PlayHT.Services
{
    public interface IStateManagementService
    {
        Task SaveDraftAsync(string userId, VoiceAgent agent);
        Task<VoiceAgent> LoadDraftAsync(string userId);
        Task DeleteDraftAsync(string userId);
    }
}

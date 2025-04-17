using System.Text.Json.Serialization;

namespace PlayHT.Models
{
    public class Voice
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        [JsonPropertyName("accent")]
        public string Accent { get; set; }
        [JsonPropertyName("style")]
        public string Style { get; set; } 
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }
        [JsonPropertyName("sample_url")]
        public string SampleUrl { get; set; }
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) &&
                   !string.IsNullOrEmpty(GetCleanVoiceId());
        }
        public string GetCleanVoiceId()
        {
            if (!string.IsNullOrEmpty(VoiceId))
                return VoiceId;
            if (!string.IsNullOrEmpty(Id))
                return Id;
            return "s3://voice-cloning-zero-shot/d9ff78ba-d016-47f6-b0ef-dd630f59414e/female-cs/manifest.json";
        }
    }
}

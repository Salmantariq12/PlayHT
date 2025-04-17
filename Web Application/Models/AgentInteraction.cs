namespace PlayHT.Models
{
    public class AgentInteraction
    {
        public string Id { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public string Status { get; set; }
        public string TranscriptUrl { get; set; }
    }
}

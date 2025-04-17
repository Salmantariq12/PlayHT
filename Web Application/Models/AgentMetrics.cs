namespace PlayHT.Models
{
    public class AgentMetrics
    {
        public int TotalCalls { get; set; }
        public int TotalMinutes { get; set; }
        public double AverageCallDuration { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public DateTime LastActive { get; set; }
    }
}

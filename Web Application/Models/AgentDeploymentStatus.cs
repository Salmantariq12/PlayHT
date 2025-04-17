namespace PlayHT.Models
{
    public class AgentDeploymentStatus
    {
        public string AgentId { get; set; }
        public string Status { get; set; }
        public string WebUrl { get; set; }
        public bool IsReady { get; set; }
        public string EmbedCode { get; set; }
    }
}

using PlayHT.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace PlayHT.Models
{
    public class VoiceAgent
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot be longer than 50 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Voice selection is required")]
        public string SelectedVoiceId { get; set; }

        [Required(ErrorMessage = "Greeting text is required")]
        [StringLength(500, ErrorMessage = "Greeting text cannot be longer than 500 characters")]
        public string GreetingText { get; set; }

        [Required(ErrorMessage = "Prompt text is required")]
        [StringLength(2000, ErrorMessage = "Prompt text cannot be longer than 2000 characters")]
        public string PromptText { get; set; }

        public string WebUrl { get; set; }
        public string EmbedCode { get; set; }

        public List<string> UploadedDocuments { get; set; } = new();
        public AgentStatus Status { get; set; }
    }
}

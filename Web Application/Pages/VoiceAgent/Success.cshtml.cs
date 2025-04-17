using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlayHT.Services;

namespace PlayHT.Pages.VoiceAgent
{
    public class SuccessModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string AgentId { get; set; }
        public string Status { get; set; }
        public Models.VoiceAgent Agent { get; set; }

        private readonly IPlayHtService _playHtService;

        public SuccessModel(IPlayHtService playHtService)
        {
            _playHtService = playHtService;
            Agent = new Models.VoiceAgent();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AgentId))
                {
                    return RedirectToPage("/VoiceAgent/Create");
                }

                // Get agent status
                var agentStatus = await _playHtService.GetAgentStatusAsync(AgentId);
                Status = agentStatus.ToString();

                // Set the embed code
                Agent.EmbedCode = $@"<script type=""text/javascript"" src=""https://unpkg.com/@play-ai/web-embed""></script>
                    <script type=""text/javascript"">
                      addEventListener(""load"", () => {{
                        PlayAI.open('{AgentId}');
                      }});
                    </script>";

                return Page();
            }
            catch (Exception ex)
            {
                // Log error but still show the page with embed code
                Status = "Status Unavailable";

                // Set the embed code even if status check fails
                Agent.EmbedCode = $@"<script type=""text/javascript"" src=""https://unpkg.com/@play-ai/web-embed""></script>
                <script type=""text/javascript"">
                  addEventListener(""load"", () => {{
                    PlayAI.open('{AgentId}');
                  }});
                </script>";

                return Page();
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlayHT.Models;
using PlayHT.Services;
using System.Text.Json;

namespace PlayHT.Pages.VoiceAgent
{
    public class CreateModel : PageModel
    {
        private readonly IPlayHtService _playHtService;
        private readonly ILogger<CreateModel> _logger;
        private const string AGENT_STATE_KEY = "AgentState";

        [BindProperty]
        public Models.VoiceAgent Agent { get; set; }

        public List<Voice> Voices { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentStep { get; set; } = 1;

        public string ErrorMessage { get; private set; }

        public List<string> Accents => Voices?.Select(v => v.Accent).Distinct().ToList() ?? new List<string>();
        public List<string> Styles => Voices?.Select(v => v.Style).Distinct().ToList() ?? new List<string>();

        public CreateModel(IPlayHtService playHtService, ILogger<CreateModel> logger)
        {
            _playHtService = playHtService;
            _logger = logger;
            Agent = new Models.VoiceAgent();
        }

        private void RestoreAgentState()
        {
            try
            {
                if (TempData.Peek(AGENT_STATE_KEY) is string serializedState)
                {
                    _logger.LogInformation($"Restoring agent state: {serializedState}");
                    var restoredAgent = JsonSerializer.Deserialize<Models.VoiceAgent>(serializedState);
                    if (restoredAgent != null)
                    {
                        Agent = restoredAgent;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring agent state: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnGetAsync(int step = 1)
        {
            try
            {
                _logger.LogInformation($"OnGet - Step: {step}");
                // Validate step range
                if (step < 1 || step > 3)
                {
                    step = 1;
                }
                CurrentStep = step;
                RestoreAgentState();
                _logger.LogInformation($"After restore - Name: {Agent.Name}, VoiceId: {Agent.SelectedVoiceId}");

                if (step == 1)
                {
                    Voices = await _playHtService.GetVoicesAsync();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnGet: {ex.Message}");
                ErrorMessage = $"Error loading page: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnGetAudioStreamAsync(string url)
        {
            try
            {
                var audioBytes = await _playHtService.GetAudioStreamAsync(url);
                return File(audioBytes, "audio/mpeg");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostGenerateVoiceSampleAsync([FromBody] VoiceSampleRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating sample for voice ID: {request.VoiceId}");
                var sampleUrl = await _playHtService.GenerateVoiceSampleAsync(request.VoiceId, request.Text);
                return new JsonResult(new { url = sampleUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating sample: {ex}");
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostAsync(string direction, bool deploy = false)
        {
            try
            {
                _logger.LogInformation($"OnPost - Direction: {direction}, Deploy: {deploy}, CurrentStep: {CurrentStep}");
                _logger.LogInformation($"Form Current Step: {Request.Form["CurrentStep"]}");

                // Get existing state
                var existingAgent = new Models.VoiceAgent();
                if (TempData.Peek(AGENT_STATE_KEY) is string existingState)
                {
                    existingAgent = JsonSerializer.Deserialize<Models.VoiceAgent>(existingState);
                    _logger.LogInformation($"Retrieved existing state - Name: {existingAgent.Name}, VoiceId: {existingAgent.SelectedVoiceId}");
                }

                // Merge current form data with existing state
                if (CurrentStep == 1)
                {
                    existingAgent.Name = Agent.Name;
                    existingAgent.SelectedVoiceId = Agent.SelectedVoiceId;
                }
                else if (CurrentStep == 2)
                {
                    existingAgent.GreetingText = Agent.GreetingText;
                    existingAgent.PromptText = Agent.PromptText;
                }

                // Update the current Agent with merged data
                Agent = existingAgent;

                _logger.LogInformation($"Merged Agent State - Name: {Agent.Name}, VoiceId: {Agent.SelectedVoiceId}, " +
                                     $"Greeting: {Agent.GreetingText}, Prompt: {Agent.PromptText}");

                // Ensure CurrentStep is correctly maintained from form
                if (int.TryParse(Request.Form["CurrentStep"], out int formStep))
                {
                    CurrentStep = formStep;
                    _logger.LogInformation($"Updated CurrentStep from form: {CurrentStep}");
                }

                // Save merged state
                SaveAgentState();

                // Validate if moving forward
                if ((direction == "next" || deploy) && !ValidateCurrentStep())
                {
                    ErrorMessage = GetValidationErrorMessage();
                    if (CurrentStep == 1)
                    {
                        Voices = await _playHtService.GetVoicesAsync();
                    }
                    return Page();
                }

                // Calculate new step
                var newStep = CurrentStep;
                if (direction == "previous" && CurrentStep > 1)
                {
                    newStep = CurrentStep - 1;
                }
                else if (direction == "next" && CurrentStep < 3)
                {
                    newStep = CurrentStep + 1;
                }

                // Handle navigation
                if (newStep != CurrentStep && !deploy)
                {
                    _logger.LogInformation($"Redirecting to step {newStep}");
                    return RedirectToPage(new { step = newStep });
                }

                if (deploy)
                {
                    _logger.LogInformation($"Deploying agent with Name: {Agent.Name}, VoiceId: {Agent.SelectedVoiceId}");

                    if (string.IsNullOrEmpty(Agent.Name) || string.IsNullOrEmpty(Agent.SelectedVoiceId))
                    {
                        ErrorMessage = "Agent name and voice must be set before deployment";
                        return Page();
                    }

                    // Create agent - this will handle both agent creation and file uploads
                    var agentId = await _playHtService.CreateAgentAsync(Agent);

                    TempData.Clear();
                    return RedirectToPage("./Success", new { agentId });
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnPost: {ex.Message}");
                ErrorMessage = ex.Message;
                if (CurrentStep == 1)
                {
                    Voices = await _playHtService.GetVoicesAsync();
                }
                return Page();
            }
        }
        public async Task<IActionResult> OnPostUploadDocumentAsync()
        {
            try
            {
                _logger.LogInformation("Handling file upload");

                // Restore current state
                RestoreAgentState();

                var file = Request.Form.Files.FirstOrDefault();
                if (file == null)
                {
                    return new JsonResult(new { error = "No file provided" }) { StatusCode = 400 };
                }

                // Store file temporarily until deployment
                var tempFilePath = Path.Combine(Path.GetTempPath(), file.FileName);

                // Check if file already exists in uploaded documents
                if (Agent.UploadedDocuments?.Contains(tempFilePath) == true)
                {
                    _logger.LogInformation($"File {file.FileName} already uploaded, skipping duplicate");
                    return new JsonResult(new { message = "File already uploaded" });
                }

                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Initialize list if null
                Agent.UploadedDocuments ??= new List<string>();

                // Add the new file
                Agent.UploadedDocuments.Add(tempFilePath);
                _logger.LogInformation($"Added file {file.FileName} to uploaded documents. Current count: {Agent.UploadedDocuments.Count}");

                // Save the updated state
                SaveAgentState();

                return new JsonResult(new { message = "File uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                return new JsonResult(new { error = ex.Message }) { StatusCode = 400 };
            }
        }
        private void SaveAgentState()
        {
            try
            {
                _logger.LogInformation($"Saving agent state - Name: {Agent.Name}, VoiceId: {Agent.SelectedVoiceId}, " +
                                     $"Greeting: {Agent.GreetingText}, Prompt: {Agent.PromptText}, ");

                var serializedState = JsonSerializer.Serialize(Agent);
                TempData[AGENT_STATE_KEY] = serializedState;
                // Ensure TempData persists
                TempData.Keep(AGENT_STATE_KEY);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving agent state: {ex.Message}");
            }
        }

        private bool ValidateCurrentStep()
        {
            _logger.LogInformation($"Validating Step {CurrentStep}");
            _logger.LogInformation($"Agent Name: {Agent.Name}");
            _logger.LogInformation($"Agent VoiceId: {Agent.SelectedVoiceId}");

            return CurrentStep switch
            {
                1 => !string.IsNullOrWhiteSpace(Agent.Name) && !string.IsNullOrWhiteSpace(Agent.SelectedVoiceId),
                2 => !string.IsNullOrWhiteSpace(Agent.GreetingText) && !string.IsNullOrWhiteSpace(Agent.PromptText),
                3 => true, // Optional step
                _ => false
            };
        }

        public string GetStepName(int step)
        {
            return step switch
            {
                1 => "Identity",
                2 => "Behavior",
                3 => "Knowledge",
                _ => throw new ArgumentOutOfRangeException(nameof(step))
            };
        }

        private string GetValidationErrorMessage()
        {
            return CurrentStep switch
            {
                1 => $"Please enter an agent name and select a voice. Name: {Agent.Name}, VoiceId: {Agent.SelectedVoiceId}",
                2 => $"Please fill in all required fields. Greeting: {Agent.GreetingText}, Prompt: {Agent.PromptText}",
                3 => "Please complete the form before proceeding.",
                _ => "Please fill in all required fields."
            };
        }
    }

    public class VoiceSampleRequest
    {
        public string VoiceId { get; set; }
        public string Text { get; set; }
    }
}
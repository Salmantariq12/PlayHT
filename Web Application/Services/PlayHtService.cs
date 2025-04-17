using PlayHT.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using PlayHT.Models.Enums;

namespace PlayHT.Services
{
    public class PlayHtService : IPlayHtService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PlayHtService> _logger;
        private readonly string _baseUrlV1 = "https://api.play.ht/api/v1";
        private readonly string _baseUrlV2 = "https://api.play.ht/api/v2";
        public PlayHtService(
         HttpClient httpClient,
         IConfiguration configuration,
         IMemoryCache cache,
         ILogger<PlayHtService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
            ConfigureHttpClient();
        }
        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            var secret = _configuration["PlayHt:Secret"];
            var userId = _configuration["PlayHt:User"];

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(userId))
            {
                throw new PlayHtException("PlayHT API credentials not found in configuration.");
            }

            _httpClient.DefaultRequestHeaders.Add("X-USER-ID", userId);
            _httpClient.DefaultRequestHeaders.Add("AUTHORIZATION", secret);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<AgentDeploymentStatus> GetAgentDeploymentStatusAsync(string agentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrlV2}/agents/{agentId}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
                return new AgentDeploymentStatus
                {
                    AgentId = agentId,
                    Status = result.RootElement.GetProperty("status").GetString(),
                    WebUrl = result.RootElement.GetProperty("webUrl").GetString(),
                    EmbedCode = result.RootElement.GetProperty("embedCode").GetString(),
                    IsReady = result.RootElement.GetProperty("isReady").GetBoolean()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent deployment status");
                throw new PlayHtException("Failed to get agent deployment status", "GET_STATUS_ERROR");
            }
        }
        public async Task<AgentStatus> GetAgentStatusAsync(string agentId)
        {
            try
            {
                var message = new HttpRequestMessage(HttpMethod.Get, $"https://api.play.ai/api/v1/agents/{agentId}");

                message.Headers.Clear();
                message.Headers.Add("Authorization", _configuration["PlayHt:AgentSecret"]);
                message.Headers.Add("X-User-ID", _configuration["PlayHt:AgentUser"]);
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(message);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Agent Status Response: {responseContent}");

                var result = JsonSerializer.Deserialize<JsonDocument>(responseContent);

                // If we can get the agent's ID and displayName, consider it active
                if (result.RootElement.TryGetProperty("id", out _) &&
                    result.RootElement.TryGetProperty("displayName", out _))
                {
                    return AgentStatus.Active;
                }

                return AgentStatus.Inactive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent status");
                return AgentStatus.Inactive;
            }
        }
        public async Task<List<Voice>> GetVoicesAsync()
        {
            const string cacheKey = "voice_list";

            if (_cache.TryGetValue(cacheKey, out List<Voice> cachedVoices))
            {
                return cachedVoices;
            }

            try
            {
                _logger.LogInformation("Fetching voices from PlayHT API");
                var response = await _httpClient.GetAsync($"{_baseUrlV2}/voices");

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new PlayHtException(
                        $"Failed to fetch voices: {content}",
                        "FETCH_VOICES_FAILED",
                        response.StatusCode);
                }

                var voices = await response.Content.ReadFromJsonAsync<List<Voice>>();
                if (voices == null)
                {
                    throw new PlayHtException("Received null response from voice API");
                }

                _cache.Set(cacheKey, voices, TimeSpan.FromHours(1));
                return voices;
            }
            catch (Exception ex) when (ex is not PlayHtException)
            {
                _logger.LogError(ex, "Error fetching voices");
                throw new PlayHtException("Failed to fetch voices", "FETCH_VOICES_ERROR");
            }
        }

        public async Task<string> GenerateVoiceSampleAsync(string voiceId, string text)
        {
            try
            {
                var request = new
                {
                    text = text,
                    voice = voiceId,
                    voice_engine = "PlayHT2.0",
                    quality = "draft",
                    output_format = "mp3"
                };

                using var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"{_baseUrlV2}/tts/stream")
                };

                var json = JsonSerializer.Serialize(request);
                httpRequestMessage.Content = new StringContent(json);
                httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await _httpClient.SendAsync(httpRequestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"API Response: {responseContent}");

                response.EnsureSuccessStatusCode();
                var result = JsonSerializer.Deserialize<JsonDocument>(responseContent);
                var audioUrl = result.RootElement.GetProperty("href").GetString();

                // Get the actual audio content
                var audioResponse = await _httpClient.GetAsync(audioUrl);
                audioResponse.EnsureSuccessStatusCode();

                // Return the URL of the actual audio content
                return audioResponse.RequestMessage.RequestUri.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateVoiceSample: {ex.Message}");
                Console.WriteLine($"Request for VoiceId: {voiceId}");
                Console.WriteLine($"Full error: {ex}");
                throw;
            }
        }
        public async Task<byte[]> GetAudioStreamAsync(string audioUrl)
        {
            var response = await _httpClient.GetAsync(audioUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        public async Task<string> CreateAgentAsync(VoiceAgent agent)
        {
            try
            {
                // Read the content of all uploaded documents
                var criticalKnowledge = new StringBuilder("This is an AI voice agent created for communication.");
                if (agent.UploadedDocuments?.Any() == true)
                {
                    foreach (var filePath in agent.UploadedDocuments)
                    {
                        var fileContent = await File.ReadAllTextAsync(filePath);
                        criticalKnowledge.AppendLine(fileContent);
                    }
                }

                var request = new
                {
                    voice = agent.SelectedVoiceId,
                    voiceSpeed = 1.2,
                    displayName = agent.Name,
                    description = $"{agent.Name} is an AI voice agent",
                    greeting = agent.GreetingText?.Split(',')[0],
                    prompt = agent.PromptText?.Split(',')[0],
                    criticalKnowledge = criticalKnowledge.ToString(),
                    visibility = "public",
                    answerOnlyFromCriticalKnowledge = agent.UploadedDocuments?.Any() ?? false,
                    llm = (string)null,
                    actions = new string[] { }
                };

                using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.play.ai/api/v1/agents");
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );
                message.Content = content;

                // Use the different keys for agent API
                message.Headers.Add("AUTHORIZATION", _configuration["PlayHt:AgentSecret"]);
                message.Headers.Add("X-USER-ID", _configuration["PlayHt:AgentUser"]);
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Console.WriteLine($"Creating agent with request: {JsonSerializer.Serialize(request)}");
                Console.WriteLine("Headers:");
                foreach (var header in message.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                var response = await _httpClient.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create Agent Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonDocument>(responseContent);
                    var agentId = result.RootElement.GetProperty("id").GetString();

                    // Generate web embed code after getting agent ID
                    agent.EmbedCode = $@"<script type=""text/javascript"" src=""https://unpkg.com/@play-ai/web-embed""></script>
                    <script type=""text/javascript"">
                      addEventListener(""load"", () => {{
                        PlayAI.open('{agentId}');
                      }});
                    </script>";

                    return agentId;
                }
                else
                {
                    Console.WriteLine($"Failed with status code: {response.StatusCode}");
                    Console.WriteLine($"Response content: {responseContent}");
                    throw new HttpRequestException($"Failed to create agent: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                agent.UploadedDocuments?.Clear();
                Console.WriteLine($"Error creating agent: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

    }
    public class PlayHtException : Exception
    {
        public string ErrorCode { get; }
        public HttpStatusCode? StatusCode { get; }

        public PlayHtException(string message, string errorCode = null, HttpStatusCode? statusCode = null)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}

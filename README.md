# PlayHT
  A C# library/application for integrating Play.ht's Text-to-Speech API for high-quality voice synthesis.

  ## Features

  - 🗣️ High-quality text-to-speech conversion
  - 🎭 Multiple voice options
  - 🌍 Multi-language support
  - ⚡ Async API calls
  - 📁 Audio file export

  ## Tech Stack

  - C#
  - .NET
  - Play.ht API
  - REST/HTTP Client

  ## Prerequisites

  - .NET 6.0 or later
  - Play.ht API key ([Get one here](https://play.ht))

  ## Getting Started

  1. Clone the repository
  ```bash
  git clone https://github.com/Salmantariq12/PlayHT.git

  2. Add your API credentials
  var client = new PlayHTClient("your-api-key", "your-user-id");

  3. Generate speech
  var audioUrl = await client.GenerateSpeechAsync("Hello, world!");

  Usage Example

  // Initialize client
  var playHT = new PlayHTClient(apiKey, userId);

  // Convert text to speech
  var result = await playHT.TextToSpeechAsync(new TTSRequest
  {
      Text = "Welcome to my application!",
      Voice = "en-US-JennyNeural",
      Quality = "high"
  });

  // Download or stream the audio
  await playHT.DownloadAudioAsync(result.AudioUrl, "output.mp3");

  Author

  Salman Tariq
  - GitHub: https://github.com/Salmantariq12
  - LinkedIn: https://www.linkedin.com/in/salman-tariq-47089592

  License

  MIT License

  ---

using Whisper.net;
using LocalVoiceAssistant.Interfaces;

namespace LocalVoiceAssistant.Services
{
    public class SpeechToTextService : ISpeechToTextService
    {
        private WhisperFactory? _factory;
        private WhisperProcessor? _processor;
        private readonly string _modelName = "ggml-base.en.bin";

        public async Task InitializeAsync()
        {
            // Auto-download the Whisper model if it doesn't exist
            if (!File.Exists(_modelName))
            {
                Console.WriteLine($"Downloading Whisper model {_modelName} from HuggingFace (approx 140MB). This only happens once...");
                using var httpClient = new HttpClient();
                await using var modelStream = await httpClient.GetStreamAsync("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin");
                await using var fileWriter = File.OpenWrite(_modelName);
                await modelStream.CopyToAsync(fileWriter);
            }

            _factory = WhisperFactory.FromPath(_modelName);
            _processor = _factory.CreateBuilder().WithLanguage("en").Build();
        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
                return string.Empty;

            using var fileStream = File.OpenRead(audioFilePath);
            string resultText = "";
            
            await foreach (var result in _processor!.ProcessAsync(fileStream))
            {
                resultText += result.Text + " ";
            }
            
            return resultText.Trim();
        }
    }
}

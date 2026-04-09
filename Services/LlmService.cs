using System.Text;
using System.Text.Json;
using LocalVoiceAssistant.Interfaces;

namespace LocalVoiceAssistant.Services
{
    public class LlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;

        public LlmService(string modelName = "llama3.2")
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:11434");
            _modelName = modelName;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = _modelName,
                    prompt,
                    system = "You are a conversational voice assistant. Your responses will be spoken aloud, so you MUST keep them extremely concise. Never output complicated lists or markdown formatting. Limit your response to 2 to 3 short sentences maximum.",
                    stream = false,
                    options = new { num_predict = 150 }
                }),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/api/generate", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                
                using var document = JsonDocument.Parse(responseString);
                return document.RootElement.GetProperty("response").GetString() ?? "";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"\n[Error communicating with Ollama: {ex.Message}]");
                Console.WriteLine("Make sure Ollama is running, and that you have downloaded the model (e.g. 'ollama run llama3')");
                return "I'm sorry, my brain is currently offline.";
            }
        }
    }
}

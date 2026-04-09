namespace LocalVoiceAssistant.Interfaces
{
    public interface ILlmService
    {
        Task<string> GenerateResponseAsync(string prompt);
    }
}

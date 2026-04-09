namespace LocalVoiceAssistant.Interfaces
{
    public interface ISpeechToTextService
    {
        Task InitializeAsync();
        Task<string> TranscribeAsync(string audioFilePath);
    }
}

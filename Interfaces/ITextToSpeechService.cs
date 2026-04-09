namespace LocalVoiceAssistant.Interfaces
{
    public interface ITextToSpeechService
    {
        void Speak(string text);
        void StopSpeaking();
    }
}

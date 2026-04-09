namespace LocalVoiceAssistant.Interfaces
{
    public interface ICommandProcessor
    {
        bool TryProcessCommand(string text, out string response);
    }
}

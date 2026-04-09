namespace LocalVoiceAssistant.Interfaces
{
    public interface IWakeWordListener
    {
        event EventHandler WakeWordDetected;
        void StartListening();
        void StopListening();
    }
}

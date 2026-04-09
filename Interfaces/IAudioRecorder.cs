namespace LocalVoiceAssistant.Interfaces
{
    public interface IAudioRecorder
    {
        event EventHandler? SilenceDetected;
        void StartRecording(string outputPath);
        void StopRecording();
        Task StopRecordingAsync();
    }
}

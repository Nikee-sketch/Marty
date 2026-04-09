namespace LocalVoiceAssistant.Interfaces
{
    public interface ISystemAutomation
    {
        void LockScreen();
        void TurnOffScreen();
        void EmptyRecycleBin();
        void SetVolume(int percentage);
        void KillApp(string processNameMatch);
        void SetDoNotDisturb(bool enable);
        void LaunchWebsite(string url);
        void CloseCurrentWindow();
        void LaunchApp(string appName);
    }
}

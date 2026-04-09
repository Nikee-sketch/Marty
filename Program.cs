using LocalVoiceAssistant.Core;
using LocalVoiceAssistant.Services;

namespace LocalVoiceAssistant
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 1. Manual Dependency Injection
            var systemAutomation = new SystemAutomation();
            
            var engine = new AssistantEngine(
                new AudioRecorder(),
                new SpeechToTextService(),
                new LlmService(),
                new TextToSpeechService(),
                new WakeWordListener(),
                new CommandProcessor(systemAutomation)
            );

            // 2. Launch App with injected Engine
            Application.Run(new MainForm(engine));
        }   
    }
}
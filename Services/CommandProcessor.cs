using System.Text.RegularExpressions;
using LocalVoiceAssistant.Interfaces;

namespace LocalVoiceAssistant.Services
{
    public class CommandProcessor : ICommandProcessor
    {
        private readonly ISystemAutomation _system;

        public CommandProcessor(ISystemAutomation system)
        {
            _system = system;
        }

        public bool TryProcessCommand(string text, out string response)
        {
            text = text.ToLowerInvariant().Trim();
            // Erase most punctuation so basic string contains match works nicely
            text = Regex.Replace(text, @"[^\w\s]", "");

            // 1. Safety Bans First
            if (text.Contains("restart") || text.Contains("turn off computer") || text.Contains("shut down") || text.Contains("shutdown") || text.Contains("sleep computer")) {
                response = "I am restricted from restarting, shutting down, or explicitly sleeping the computer for your safety.";
                return true;
            }
            if (text.Contains("delete") && !text.Contains("recycle bin") || text.Contains("move") || text.Contains("rename")) {
                response = "I am restricted from moving, deleting, or renaming specific files for data safety.";
                return true;
            }

            // 2. Applications
            bool isLaunchCmd = text.StartsWith("open") || text.StartsWith("launch") || text.StartsWith("start");

            if (text == "calculator" || text.Contains("calculator") && isLaunchCmd) {
                _system.LaunchApp("calculator");
                response = "Opening Calculator.";
                return true;
            }
            if (text == "file explorer" || text == "explorer" || text.Contains("explorer") && isLaunchCmd) {
                _system.LaunchApp("explorer");
                response = "Opening File Explorer.";
                return true;
            }
            if (text == "rider" || text.Contains("rider") && isLaunchCmd) {
                _system.LaunchApp("rider");
                response = "Opening JetBrains Rider.";
                return true;
            }
            if (text == "to do" || text == "todo" || text.Contains("todo") && isLaunchCmd || text.Contains("to do") && isLaunchCmd) {
                _system.LaunchApp("todo");
                response = "Opening Microsoft To Do.";
                return true;
            }
            if (text == "telegram" || text.Contains("telegram") && isLaunchCmd) {
                _system.LaunchApp("telegram");
                response = "Opening Telegram.";
                return true;
            }
            if (text == "chrome" || (text.Contains("chrome") && isLaunchCmd)) {
                _system.LaunchApp("chrome");
                response = "Opening Google Chrome.";
                return true;
            }

            // 4. System UI & Operations
            if (text.Contains("close window") || text.Contains("close application") || text.Contains("close this app") || text.Contains("close active window")) {
                _system.CloseCurrentWindow();
                response = "Window closed.";
                return true;
            }
            if (text.Contains("empty recycle bin") || text.Contains("clear recycle bin")) {
                _system.EmptyRecycleBin();
                response = "Recycle bin has been emptied.";
                return true;
            }

            // 5. Power State
            if (text.Contains("lock screen") || text.Contains("lock pc") || text.Contains("lock computer")) {
                _system.LockScreen();
                response = "Locking the screen.";
                return true;
            }
            if (text.Contains("turn off screen") || text.Contains("turn off monitor") || text.Contains("sleep screen")) {
                _system.TurnOffScreen();
                response = "Turning off screen.";
                return true;
            }

            if (text.Contains("turn on music") || text.Contains("play music") || text.Contains("music"))
            {
                _system.LaunchWebsite("https://www.youtube.com/watch?v=JBkqwmKQspA&list=RDJBkqwmKQspA&start_radio=1");
                _system.LaunchWebsite("https://music.yandex.uz/");
                _system.SetVolume(35);
                
                response = "Here we go again. Volume set to 35 percent";
                return true;
            }
            
            // 6. Automation Modes
            if (text.Contains("concentration mode") || text.Contains("focus mode") || text.Contains("work mode")) {
                _system.LaunchApp("todo");
                _system.SetDoNotDisturb(true);
                _system.SetVolume(10);
                _system.KillApp("telegram"); 
                
                response = "Concentration Mode activated. To Do opened, volume set to 10 percent, notifications paused. Focus up!";
                return true;
            }
            if (text.Contains("pleasure mode") || text.Contains("relaxation mode") || text.Contains("chill mode")) {
                _system.LaunchWebsite("https://youtube.com");
                _system.SetVolume(40);
                _system.SetDoNotDisturb(false);
                _system.KillApp("rider"); 
                _system.KillApp("todo");
                
                response = "Pleasure Mode activated. Volume set to 40 percent, YouTube is opening, and work applications have been closed. Enjoy!";
                return true;
            }

            // Unhandled by rules, fallback to LLM
            response = string.Empty;
            return false;
        }
    }
}

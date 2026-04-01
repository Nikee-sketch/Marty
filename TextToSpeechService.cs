using System.Diagnostics;

namespace LocalVoiceAssistant
{
    public class TextToSpeechService
    {
        private Process _currentProcess;

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // Escape quotes inside the text for PowerShell execution
            text = text.Replace("'", "''");
            
            // Remove markdown asterisks which sound weird when spoken
            text = text.Replace("*", "");

            // Use built-in Windows speech synthesis via PowerShell
            var script = $"Add-Type -AssemblyName System.Speech; " +
                         $"$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                         $"$synth.Speak('{text}')";

            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            lock (this)
            {
                _currentProcess = Process.Start(processInfo);
            }
            
            _currentProcess?.WaitForExit();
            
            lock (this)
            {
                _currentProcess = null;
            }
        }

        public void StopSpeaking()
        {
            lock (this)
            {
                if (_currentProcess != null && !_currentProcess.HasExited)
                {
                    try
                    {
                        // Safely kill the powershell process
                        _currentProcess.Kill(true);
                    }
                    catch { }
                }
            }
        }
    }
}

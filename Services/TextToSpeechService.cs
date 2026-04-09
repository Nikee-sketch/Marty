using System;
using System.Speech.Synthesis;
using LocalVoiceAssistant.Interfaces;

namespace LocalVoiceAssistant.Services
{
    public class TextToSpeechService : ITextToSpeechService, IDisposable
    {
        private readonly SpeechSynthesizer _synth;

        public TextToSpeechService()
        {
            _synth = new SpeechSynthesizer();
            // Set default voice or properties here if needed
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // Remove markdown asterisks which sound weird when spoken
            text = text.Replace("*", "");

            // Native SpeechSynthesizer blocks until done, which matches the previous powershell script logic
            _synth.Speak(text);
        }

        public void StopSpeaking()
        {
             _synth.SpeakAsyncCancelAll();
        }

        public void Dispose()
        {
             _synth.Dispose();
        }
    }
}

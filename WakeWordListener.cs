using System;
using System.Speech.Recognition;

namespace LocalVoiceAssistant
{
    public class WakeWordListener
    {
        private SpeechRecognitionEngine _recognizer;
        public event EventHandler WakeWordDetected;

        public WakeWordListener()
        {
            _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
            
            var choices = new Choices(new string[] { "Hey", "Hi", "Hello", "Listen" });
            var grammarBuilder = new GrammarBuilder(choices);
            
            var grammar = new Grammar(grammarBuilder);

            _recognizer.LoadGrammar(grammar);
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            
            try 
            {
                _recognizer.SetInputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not set microphone input for wake word: " + ex.Message);
            }
        }

        public void StartListening()
        {
            // RecognizeAsync(RecognizeMode.Multiple) runs in background endlessly
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void StopListening()
        {
            _recognizer.RecognizeAsyncStop();
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // If the acoustic confidence of the wake word is decently high
            if (e.Result.Confidence > 0.6f)
            {
                WakeWordDetected?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

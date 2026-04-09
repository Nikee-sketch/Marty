using System;
using System.IO;
using System.Threading.Tasks;
using LocalVoiceAssistant.Interfaces;

namespace LocalVoiceAssistant.Core
{
    public class AssistantEngine
    {
        private readonly IAudioRecorder _recorder;
        private readonly ISpeechToTextService _stt;
        private readonly ILlmService _llm;
        private readonly ITextToSpeechService _tts;
        private readonly IWakeWordListener _wakeWord;
        private readonly ICommandProcessor _cmdProcessor;
        
        private bool _isRecording;
        private bool _isSpeaking;
        private readonly string _tempAudioPath = "recording.wav";

        public event EventHandler<string>? StateChanged;
        public event EventHandler<(string speaker, string message)>? MessageLogged;
        public event EventHandler<bool>? RecordingStateChanged;
        public event EventHandler<bool>? SpeakingStateChanged;

        public AssistantEngine(
            IAudioRecorder recorder,
            ISpeechToTextService stt,
            ILlmService llm,
            ITextToSpeechService tts,
            IWakeWordListener wakeWord,
            ICommandProcessor cmdProcessor)
        {
            _recorder = recorder;
            _stt = stt;
            _llm = llm;
            _tts = tts;
            _wakeWord = wakeWord;
            _cmdProcessor = cmdProcessor;

            _recorder.SilenceDetected += OnSilenceDetected;
            _wakeWord.WakeWordDetected += OnWakeWordDetected;
        }

        public async Task InitializeAsync()
        {
            Log("System", "Initializing Local Voice Assistant...");
            Log("System", "Loading Whisper and LLaMA models into memory...");
            Log("System", "Please wait... This might take a few moments depending on your hardware.\n");

            try
            {
                await _stt.InitializeAsync();
                
                Log("System", "Assistant is ready! Say 'Hey' or 'Listen' to start.");
                SetState("Ready");
                _wakeWord.StartListening();
            }
            catch (Exception ex)
            {
                Log("System (Error)", $"Error initializing: {ex.Message}");
                SetState("Initialization Error");
            }
        }

        private void OnWakeWordDetected(object? sender, EventArgs e)
        {
            if (!_isRecording && !_isSpeaking)
            {
                Log("System", "Wake word detected!");
                ToggleRecording(); // Start recording
            }
        }

        private void OnSilenceDetected(object? sender, EventArgs e)
        {
            if (_isRecording)
            {
                Log("System", "Silence detected. Stopping recording based on VAD...");
                ToggleRecording(); // Stop recording
            }
        }

        public void StopSpeaking()
        {
             if (_isSpeaking)
             {
                  _tts.StopSpeaking();
                  Log("System", "AI speech interrupted.");
             }
        }

        public async void ToggleRecording()
        {
            if (_isSpeaking)
            {
                StopSpeaking();
                return;
            }

            if (!_isRecording)
            {
                // Start physical recording
                _isRecording = true;
                RecordingStateChanged?.Invoke(this, true);
                SetState("Recording...");
                
                _wakeWord.StopListening(); // Pause wake word so we don't pick it up again while talking
                _recorder.StartRecording(_tempAudioPath);
                
                Log("System", "🔴 Recording started... Speak now. (Will auto-stop on silence)");
            }
            else
            {
                // Stop and process
                _isRecording = false;
                RecordingStateChanged?.Invoke(this, false);
                SetState("Processing...");
                
                await _recorder.StopRecordingAsync();
                Log("System", "Recording stopped. Transcribing...");

                await ProcessAudioAsync();

                SetState("Ready");
                _wakeWord.StartListening(); // Resume wake word detection
            }
        }

        private async Task ProcessAudioAsync()
        {
            try
            {
                string userPrompt = await _stt.TranscribeAsync(_tempAudioPath);
                if (string.IsNullOrWhiteSpace(userPrompt))
                {
                    Log("System", "Could not hear you clearly.");
                    return;
                }
                
                Log("You", userPrompt);

                string aiResponse;
                if (_cmdProcessor.TryProcessCommand(userPrompt, out string cmdResponse))
                {
                    Log("System", "Executed Local Command");
                    aiResponse = cmdResponse;
                }
                else
                {
                    Log("System", "Thinking...");
                    aiResponse = await _llm.GenerateResponseAsync(userPrompt);
                }
                
                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    Log("Assistant", aiResponse);
                    
                    _isSpeaking = true;
                    SpeakingStateChanged?.Invoke(this, true);
                    
                    // Run TTS asynchronously so we don't block
                    await Task.Run(() => _tts.Speak(aiResponse));
                    
                    _isSpeaking = false;
                    SpeakingStateChanged?.Invoke(this, false);
                }
            }
            catch (Exception ex)
            {
                Log("System (Error)", ex.Message);
            }
            finally
            {
                if (File.Exists(_tempAudioPath))
                {
                    try { File.Delete(_tempAudioPath); } catch { /* ignore */ }
                }
            }
        }

        public async Task ShutdownAsync()
        {
             if (_isRecording)
             {
                  try { await _recorder.StopRecordingAsync(); } catch { }
             }
             if (File.Exists(_tempAudioPath))
             {
                 try { File.Delete(_tempAudioPath); } catch { }
             }
        }

        private void Log(string speaker, string message)
        {
            MessageLogged?.Invoke(this, (speaker, message));
        }

        private void SetState(string state)
        {
            StateChanged?.Invoke(this, state);
        }
    }
}

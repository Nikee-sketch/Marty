using NAudio.Wave;
using LocalVoiceAssistant.Interfaces;

namespace LocalVoiceAssistant.Services
{
    public class AudioRecorder : IAudioRecorder
    {
        private WaveInEvent _waveIn;
        private WaveFileWriter? _writer;
        private bool _isRecording;
        
        // VAD parameters
        private int _silenceDurationMs;
        private const int SilenceThresholdRms = 200; // Calibrated volume threshold
        private const int MaxSilenceDurationMs = 2000; // 1.5 seconds of silence stops recording
        
        public event EventHandler? SilenceDetected;

        public AudioRecorder()
        {
            _waveIn = new WaveInEvent
            {
                // Whisper requires 16000Hz, 16-bit, Mono audio
                WaveFormat = new WaveFormat(16000, 16, 1) 
            };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
        }

        public void StartRecording(string outputPath)
        {
            _silenceDurationMs = 0; // Reset VAD
            _writer = new WaveFileWriter(outputPath, _waveIn.WaveFormat);
            _isRecording = true;
            _waveIn.StartRecording();
        }

        public void StopRecording()
        {
            if (_isRecording)
            {
                _waveIn.StopRecording();
                _isRecording = false;
            }
        }

        public async Task StopRecordingAsync()
        {
            if (_isRecording)
            {
                var tcs = new TaskCompletionSource<bool>();
                EventHandler<StoppedEventArgs>? handler = null;
                handler = (_, _) =>
                {
                    _waveIn.RecordingStopped -= handler;
                    tcs.TrySetResult(true);
                };
                _waveIn.RecordingStopped += handler;

                _waveIn.StopRecording();
                _isRecording = false;

                await tcs.Task;
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_writer != null)
            {
                _writer.Write(e.Buffer, 0, e.BytesRecorded);
                _writer.Flush();
                
                // Voice Activity Detection (VAD) using Root-Mean-Square (RMS)
                float rms = 0;
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                    rms += sample * sample;
                }
                
                int sampleCount = e.BytesRecorded / 2;
                if (sampleCount > 0)
                {
                    rms = (float)Math.Sqrt(rms / sampleCount);
                    
                    if (rms < SilenceThresholdRms)
                    {
                        // Add duration of this audio buffer (in ms) to silence running total
                        _silenceDurationMs += (int)((e.BytesRecorded / 2.0) / 16.0); // 16 kHz = 16 samples per ms
                        
                        if (_silenceDurationMs >= MaxSilenceDurationMs)
                        {
                            SilenceDetected?.Invoke(this, EventArgs.Empty);
                            _silenceDurationMs = 0; // Prevent repetitive firing
                        }
                    }
                    else
                    {
                        // Reset if we hear noise
                        _silenceDurationMs = 0;
                    }
                }
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}

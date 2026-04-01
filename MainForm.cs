using System.ComponentModel;

namespace LocalVoiceAssistant
{
    public class MainForm : Form
    {
        private Button _recordButton;
        private RichTextBox _chatBox;
        
        private AudioRecorder _recorder;
        private SpeechToTextService _stt;
        private LlmService _llm;
        private TextToSpeechService _tts;
        private WakeWordListener _wakeWord;
        private NotifyIcon _trayIcon;
        private CommandProcessor _cmdProcessor;
        
        private bool _isRecording;
        private bool _isSpeaking;
        private string _tempAudioPath = "recording.wav";
        private bool _isRealClose;

        public MainForm()
        {
            InitializeComponents();
            
            // Handle form closing to ensure clean shutdown or jump to tray
            FormClosing += MainForm_FormClosing;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await InitializeAssistantAsync();
        }

        private void InitializeComponents()
        {
            Text = "Local Voice Assistant";
            Size = new Size(500, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            _chatBox = new RichTextBox
            {
                Location = new Point(10, 10),
                Size = new Size(460, 480),
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_chatBox);

            _recordButton = new Button
            {
                Text = "Loading Models...",
                Location = new Point(10, 500),
                Size = new Size(460, 50),
                BackColor = Color.FromArgb(50, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _recordButton.FlatAppearance.BorderSize = 0;
            _recordButton.Click += RecordButton_Click;
            Controls.Add(_recordButton);
            
            MinimumSize = new Size(400, 500);

            // System Tray Icon
            var trayComponents = new Container();
            _trayIcon = new NotifyIcon(trayComponents)
            {
                Icon = SystemIcons.Application,
                Text = "Local Voice Assistant",
                Visible = true
            };
            _trayIcon.DoubleClick += (_, _) => { Show(); WindowState = FormWindowState.Normal; };
            
            var contextMenu = new ContextMenuStrip();
            var closeItem = new ToolStripMenuItem("Exit Assistant");
            closeItem.Click += (_, _) => {
                _isRealClose = true;
                _trayIcon.Visible = false;
                Application.Exit();
            };
            contextMenu.Items.Add(closeItem);
            _trayIcon.ContextMenuStrip = contextMenu;
        }

        private async Task InitializeAssistantAsync()
        {
            Log("System", "Initializing Local Voice Assistant...");
            Log("System", "Loading Whisper and LLaMA models into memory...");
            Log("System", "Please wait... This might take a few moments depending on your hardware.\n");
            
            _recorder = new AudioRecorder();
            _recorder.SilenceDetected += Recorder_SilenceDetected;

            _stt = new SpeechToTextService();
            _llm = new LlmService("llama3.2");
            _tts = new TextToSpeechService();
            _wakeWord = new WakeWordListener();
            _wakeWord.WakeWordDetected += WakeWord_Detected;
            _cmdProcessor = new CommandProcessor();

            try
            {
                await _stt.InitializeAsync();
                
                Log("System", "Assistant is ready! Say 'Hey' or 'Listen' to start.");
                _recordButton.Text = "Start Recording";
                _recordButton.Enabled = true;
                _recordButton.BackColor = Color.ForestGreen;
                
                _wakeWord.StartListening();
            }
            catch (Exception ex)
            {
                Log("System", $"Error initializing: {ex.Message}");
                _recordButton.Text = "Initialization Error";
            }
        }

        private void WakeWord_Detected(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => WakeWord_Detected(sender, e));
                return;
            }

            if (!_isRecording && !_isSpeaking)
            {
                if (WindowState == FormWindowState.Minimized || !Visible)
                {
                    Show();
                    WindowState = FormWindowState.Normal;
                }
                
                Log("System", "Wake word detected!");
                // Trigger recording
                RecordButton_Click(this, EventArgs.Empty);
            }
        }

        private void Recorder_SilenceDetected(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => Recorder_SilenceDetected(sender, e));
                return;
            }

            if (_isRecording)
            {
                Log("System", "Silence detected. Stopping recording based on VAD...");
                RecordButton_Click(this, EventArgs.Empty);
            }
        }

        private async void RecordButton_Click(object sender, EventArgs e)
        {
            if (_isSpeaking)
            {
                _tts.StopSpeaking();
                Log("System", "AI speech interrupted.");
                return;
            }

            if (!_isRecording)
            {
                // Start physical recording
                _isRecording = true;
                _wakeWord.StopListening(); // Pause wake word so we don't pick it up again while talking

                _recordButton.Text = "Stop Recording";
                _recordButton.BackColor = Color.Crimson;
                
                _recorder.StartRecording(_tempAudioPath);
                Log("System", "🔴 Recording started... Speak now. (Will auto-stop on silence)");
            }
            else
            {
                // Stop and process
                _isRecording = false;
                _recordButton.Text = "Processing...";
                _recordButton.Enabled = false;
                _recordButton.BackColor = Color.DarkGoldenrod;
                
                await _recorder.StopRecordingAsync();
                Log("System", "Recording stopped. Transcribing...");

                await ProcessAudioAsync();

                _recordButton.Text = "Start Recording";
                _recordButton.Enabled = true;
                _recordButton.BackColor = Color.ForestGreen;
                
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
                    if (_recordButton.InvokeRequired)
                    {
                        _recordButton.Invoke(() => {
                            _recordButton.Text = "Stop Speaking";
                            _recordButton.Enabled = true;
                            _recordButton.BackColor = Color.Orange;
                        });
                    }
                    else
                    {
                        _recordButton.Text = "Stop Speaking";
                        _recordButton.Enabled = true;
                        _recordButton.BackColor = Color.Orange;
                    }

                    // Run TTS asynchronously so UI doesn't freeze while speaking
                    await Task.Run(() => _tts.Speak(aiResponse));
                    
                    _isSpeaking = false;
                }
            }
            catch (Exception ex)
            {
                Log("System (Error)", ex.Message);
            }
            finally
            {
                if (File.Exists(_tempAudioPath))
                    File.Delete(_tempAudioPath);
            }
        }

        private void Log(string speaker, string message)
        {
            if (_chatBox.InvokeRequired)
            {
                _chatBox.Invoke(() => Log(speaker, message));
                return;
            }

            int startLength = _chatBox.TextLength;
            _chatBox.AppendText($"[{speaker}]: {message}\n\n");
            int currLength = _chatBox.TextLength;
            
            _chatBox.Select(startLength, currLength - startLength);
            if (speaker == "System") _chatBox.SelectionColor = Color.LightSlateGray;
            else if (speaker == "System (Error)") _chatBox.SelectionColor = Color.Tomato;
            else if (speaker == "You") _chatBox.SelectionColor = Color.LightSkyBlue;
            else if (speaker == "Assistant") _chatBox.SelectionColor = Color.LightGreen;
            else _chatBox.SelectionColor = Color.White;
            
            _chatBox.Select(_chatBox.TextLength, 0); 
            _chatBox.ScrollToCaret();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isRealClose)
            {
                e.Cancel = true;
                this.Hide();
                _trayIcon.ShowBalloonTip(2000, "Assistant Running", "I'm listening for the wake word in the background. Say 'Hey' to activate.", ToolTipIcon.Info);
            }
            else
            {
                if (_isRecording)
                {
                    try { _recorder.StopRecordingAsync().Wait(500); }
                    catch
                    {
                        // ignored
                    }
                }
                if (File.Exists(_tempAudioPath))
                {
                    try { File.Delete(_tempAudioPath); }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }
}

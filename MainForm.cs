using System.ComponentModel;
using LocalVoiceAssistant.Core;

namespace LocalVoiceAssistant
{
    public class MainForm : Form
    {
        private Button _recordButton;
        private RichTextBox _chatBox;
        private NotifyIcon _trayIcon;
        
        private readonly AssistantEngine _engine;
        private bool _isRealClose;

        public MainForm(AssistantEngine engine)
        {
            _engine = engine;
            
            InitializeComponents();
            WireUpEngineEvents();
            
            FormClosing += MainForm_FormClosing;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await _engine.InitializeAsync();
        }

        private void InitializeComponents()
        {
            Text = "Marty";
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
            closeItem.Click += async (_, _) => {
                _isRealClose = true;
                _trayIcon.Visible = false;
                await _engine.ShutdownAsync();
                Application.Exit();
            };
            contextMenu.Items.Add(closeItem);
            _trayIcon.ContextMenuStrip = contextMenu;
        }

        private void WireUpEngineEvents()
        {
            _engine.MessageLogged += (s, e) => Log(e.speaker, e.message);
            
            _engine.StateChanged += (s, stateStr) => 
            {
                if (stateStr == "Ready")
                {
                    UpdateRecordButton("Start Recording", Color.ForestGreen, true);
                }
                else if (stateStr == "Recording...")
                {
                    UpdateRecordButton("Stop Recording", Color.Crimson, true);
                    BringToFrontSafely();
                }
                else if (stateStr == "Processing...")
                {
                    UpdateRecordButton("Processing...", Color.DarkGoldenrod, false);
                }
                else if (stateStr == "Initialization Error")
                {
                    UpdateRecordButton("Initialization Error", Color.DarkRed, false);
                }
            };

            _engine.SpeakingStateChanged += (s, isSpeaking) =>
            {
                if (isSpeaking)
                {
                    UpdateRecordButton("Stop Speaking", Color.Orange, true);
                }
                else
                {
                    // Will naturally be reverted back to Ready by the engine
                }
            };
        }

        private void BringToFrontSafely()
        {
            if (InvokeRequired)
            {
                Invoke(BringToFrontSafely);
                return;
            }

            if (WindowState == FormWindowState.Minimized || !Visible)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void UpdateRecordButton(string text, Color backColor, bool enabled)
        {
            if (_recordButton.InvokeRequired)
            {
                _recordButton.Invoke(() => UpdateRecordButton(text, backColor, enabled));
                return;
            }
            
            _recordButton.Text = text;
            _recordButton.BackColor = backColor;
            _recordButton.Enabled = enabled;
        }

        private void RecordButton_Click(object sender, EventArgs e)
        {
            _engine.ToggleRecording();
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
        }
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NAudio.CoreAudioApi;

namespace LocalVoiceAssistant
{
    public static class SystemAutomation
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const uint WmSyscommand = 0x0112;
        private const int ScMonitorpower = 0xF170;
        private const int MonitorOff = 2;
        private const uint WmClose = 0x0010;

        [Flags]
        enum RecycleFlags : uint
        {
            SherbNoconfirmation = 1,
            SherbNoprogressui = 2,
            SherbNosound = 4
        }
        
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, RecycleFlags dwFlags);


        public static void LockScreen()
        {
            LockWorkStation();
        }

        public static void TurnOffScreen()
        {
            Form f = new Form();
            SendMessage(f.Handle, WmSyscommand, ScMonitorpower, MonitorOff);
            f.Dispose();
        }

        public static void EmptyRecycleBin()
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SherbNoconfirmation | RecycleFlags.SherbNoprogressui | RecycleFlags.SherbNosound);
        }

        public static void SetVolume(int percentage)
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = percentage / 100.0f;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not set volume: " + ex.Message);
            }
        }

        public static void KillApp(string processNameMatch)
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains(processNameMatch, StringComparison.OrdinalIgnoreCase))
                {
                    try { process.Kill(); }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        public static void SetDoNotDisturb(bool enable)
        {
            try
            {
                int value = enable ? 1 : 0; 
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings", "NOC_GLOBAL_SETTING_TOASTS_LEVEL", value);
            }
            catch
            {
                // ignored
            }
        }

        public static void LaunchWebsite(string url)
        {
            var psi = new ProcessStartInfo 
            { 
                FileName = "chrome.exe",
                Arguments = url,
                UseShellExecute = true
            };
            try { Process.Start(psi); }
            catch
            {
                // ignored
            }
        }

        public static void CloseCurrentWindow()
        {
            IntPtr handle = GetForegroundWindow();
            if (handle != IntPtr.Zero)
            {
                PostMessage(handle, WmClose, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static void LaunchApp(string appName)
        {
            var psi = new ProcessStartInfo { UseShellExecute = true };
            
            switch (appName.ToLowerInvariant())
            {
                case "calculator":
                    psi.FileName = "calc.exe";
                    break;
                case "explorer":
                    psi.FileName = "explorer.exe";
                    break;
                case "rider":
                    psi.FileName = "rider64.exe";
                    break;
                case "todo":
                    psi.FileName = "ms-todo:"; // Windows 10/11 URI scheme
                    break;
                case "telegram":
                    // Telegram is usually inside AppData. Fallback to just "telegram.exe" in PATH.
                    string telegramPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Telegram Desktop\Telegram.exe";
                    psi.FileName = File.Exists(telegramPath) ? telegramPath : "telegram.exe";
                    break;
                case "chrome":
                    psi.FileName = "chrome.exe";
                    break;
            }
            
            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not launch app: " + ex.Message);
            }
        }
        

    }
}

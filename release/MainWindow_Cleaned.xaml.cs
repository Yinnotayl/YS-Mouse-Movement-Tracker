// MainWindow.xaml.cs
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Mouse_Movement_Recorder
{
    public partial class MainWindow : Window
    {
        // for cursor position
        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        // for click & scroll emulation
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // low-level mouse hook delegate
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelMouseProc _proc;

        // constants
        const int WH_MOUSE_LL = 14;
        const int WM_MOUSEWHEEL = 0x020A;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const int WHEEL_DELTA = 120;
        const int VK_LBUTTON = 0x01;
        const int VK_RBUTTON = 0x02;

        readonly string traceFilePath = Path.Combine(
            //Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "mouse_trace.txt"
        );

        CancellationTokenSource tokenSource;
        StreamWriter writer;
        Stopwatch sw;
        bool wasLeftDown;
        bool wasRightDown;

        public MainWindow()
        {
            InitializeComponent();
            _proc = HookCallback;
        }

        private void StartHook()
        {
            var moduleHandle = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, moduleHandle, 0);
        }

        private void StopHook()
        {
            if (_hookID != IntPtr.Zero)
                UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            RecordButton.IsEnabled = false;
            PlaybackButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            writer = new StreamWriter(traceFilePath, false);
            writer.WriteLine("time_seconds:X:Y:event:data");
            sw = Stopwatch.StartNew();
            wasLeftDown = false;
            wasRightDown = false;

            StartHook();

            await Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (GetCursorPos(out POINT p))
                    {
                        double t = sw.Elapsed.TotalSeconds;
                        bool isLeft = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
                        bool isRight = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
                        string ev = "move";
                        string data = string.Empty;

                        if (isLeft && !wasLeftDown) ev = "left_down";
                        else if (!isLeft && wasLeftDown) ev = "left_up";
                        else if (isRight && !wasRightDown) ev = "right_down";
                        else if (!isRight && wasRightDown) ev = "right_up";

                        writer.WriteLine($"{t:F4}:{p.X}:{p.Y}:{ev}:{data}");
                        wasLeftDown = isLeft;
                        wasRightDown = isRight;
                    }
                    Thread.Sleep(5);
                }
                StopHook();
                writer.Flush(); writer.Close();
            });

            MessageBox.Show($"Recording saved to:\n{traceFilePath}", "Done");
            RecordButton.IsEnabled = true;
            PlaybackButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == WM_MOUSEWHEEL && writer != null)
            {
                int rawDelta = Marshal.ReadInt32(lParam + 8);
                GetCursorPos(out POINT p);
                double t = sw.Elapsed.TotalSeconds;
                writer.WriteLine($"{t:F4}:{p.X}:{p.Y}:wheel:{rawDelta}");
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) => tokenSource?.Cancel();

        private async void PlaybackButton_Click(object sender, RoutedEventArgs e)
        {
            RecordButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            PlaybackButton.IsEnabled = false;

            if (!File.Exists(traceFilePath))
            {
                MessageBox.Show("No recording found on Desktop.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                RecordButton.IsEnabled = true;
                PlaybackButton.IsEnabled = true;
                return;
            }

            var lines = File.ReadAllLines(traceFilePath);
            var records = new (double t, int x, int y, string ev, int data)[lines.Length - 1];
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(':');
                records[i - 1] = (
                    double.Parse(parts[0], CultureInfo.InvariantCulture),
                    int.Parse(parts[1]),
                    int.Parse(parts[2]),
                    parts[3],
                    string.IsNullOrEmpty(parts[4]) ? 0 : int.Parse(parts[4])
                );
            }

            await Task.Run(() =>
            {
                double prevT = 0;
                foreach (var rec in records)
                {
                    int wait = (int)((rec.t - prevT) * 1000);
                    if (wait > 0) Thread.Sleep(wait);

                    SetCursorPos(rec.x, rec.y);
                    switch (rec.ev)
                    {
                        case "left_down":
                            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)rec.x, (uint)rec.y, 0, UIntPtr.Zero);
                            break;
                        case "left_up":
                            mouse_event(MOUSEEVENTF_LEFTUP, (uint)rec.x, (uint)rec.y, 0, UIntPtr.Zero);
                            break;
                        case "right_down":
                            mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)rec.x, (uint)rec.y, 0, UIntPtr.Zero);
                            break;
                        case "right_up":
                            mouse_event(MOUSEEVENTF_RIGHTUP, (uint)rec.x, (uint)rec.y, 0, UIntPtr.Zero);
                            break;
                        case "wheel":
                            int sign = Math.Sign(rec.data);
                            mouse_event(MOUSEEVENTF_WHEEL, (uint)rec.x, (uint)rec.y, (uint)(sign * WHEEL_DELTA), UIntPtr.Zero);
                            break;
                    }
                    prevT = rec.t;
                }
            });

            MessageBox.Show("Playback complete.", "Done");
            RecordButton.IsEnabled = true;
            PlaybackButton.IsEnabled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            tokenSource?.Cancel();
            StopHook();
            base.OnClosed(e);
        }
    }
}

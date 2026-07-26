using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Speech.Synthesis;

namespace lanpingcj
{
    public partial class MessageBox : Window
    {
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private const int VOICE_RATE = 0; // 语音速率，0为正常速度
        private const string DEFAULT_VOICE_NAME = "Microsoft Yaoyao";

        public string? NewTittle { get; set; }
        public string? NewContent { get; set; }
        public string? New_extra_text { get; set; }
        public string? studentsName { get; set; }
        public bool AutoApplyProperties { get; set; } = true;

        // 直接读取当前配置文件，保证设置页的开关立即生效
        public bool TTS_open;
        public bool SoundEnabled;

        private SpeechSynthesizer? _synthesizer;

        public MessageBox()
        {
            InitializeComponent();

            var cfg = ConfigService.LoadCurrent();
            TTS_open = cfg.TTS;
            SoundEnabled = cfg.Sound;

            if (AutoApplyProperties)
            {
                this.Loaded += (s, e) => ApplyPropertiesToUI();
            }

            if (SoundEnabled)
            {
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch (SystemException ex)
                {
                    Debug.WriteLine($"播放声音失败: {ex.Message}");
                }
            }

            SourceInitialized += AWindow_SourceInitialized;
            // 订阅会话切换事件（处理锁屏）
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            // 初始化语音合成器（非阻塞播放）
            if (TTS_open)
            {
                try
                {
                    _synthesizer = new SpeechSynthesizer();
                    _synthesizer.SetOutputToDefaultAudioDevice();
                }
                catch (SystemException ex)
                {
                    Debug.WriteLine($"初始化 SpeechSynthesizer 失败: {ex.Message}");
                    _synthesizer = null;
                }
            }
        }

        private void AWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // 设置窗口扩展样式为工具窗口
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // 设置窗口为系统级置顶
            SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            this.Topmost = true;
        }

        private void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                // 锁屏时取消置顶，避免遮挡登录界面
                case Microsoft.Win32.SessionSwitchReason.SessionLock:
                    this.Topmost = false;
                    break;
                case Microsoft.Win32.SessionSwitchReason.SessionUnlock:
                    this.Topmost = true;
                    var hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    }
                    break;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            try
            {
                _synthesizer?.SpeakAsyncCancelAll();
                _synthesizer?.Dispose();
            }
            catch (SystemException ex)
            {
                Debug.WriteLine($"释放 SpeechSynthesizer 失败: {ex.Message}");
            }
            _synthesizer = null;
            base.OnClosed(e);
        }

        public void ApplyPropertiesToUI()
        {
            extra_text.Text = New_extra_text;
            test_tittle.Text = NewTittle;
            Context.Text = NewContent;

            // 使用异步播放，避免阻塞 UI 线程
            try
            {
                if (TTS_open && _synthesizer != null && !string.IsNullOrWhiteSpace(studentsName))
                {
                    _synthesizer.Rate = VOICE_RATE;

                    // 优先选择指定语音，找不到则使用系统默认语音
                    foreach (var voice in _synthesizer.GetInstalledVoices())
                    {
                        if (voice.VoiceInfo.Name.Contains(DEFAULT_VOICE_NAME))
                        {
                            _synthesizer.SelectVoice(voice.VoiceInfo.Name);
                            break;
                        }
                    }

                    // 取消任何未完成的异步播放，避免重叠
                    _synthesizer.SpeakAsyncCancelAll();
                    _synthesizer.SpeakAsync(studentsName);
                }
            }
            catch (SystemException ex)
            {
                Debug.WriteLine($"语音播放失败: {ex.Message}");
            }
        }

        void OK(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Wpf.Ui.Controls;
using System.Speech.Synthesis;

namespace lanpingcj
{
    public partial class Window3 : Window, IDisposable
    {
        // 常量定义
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;

        // Win32 API 声明 - 新增的窗口样式相关常量和方法
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const double TITLE_HEIGHT = 75;
        private const double MIN_WINDOW_HEIGHT = 150;
        private const double BOTTOM_BUTTON_HEIGHT = 60;
        private const double TEXT_WIDTH = 290;
        private const double DEFAULT_WINDOW_HEIGHT = 250;
        private const int VOICE_RATE = 0; // 语音速率，0为正常速度
        private const string DEFAULT_VOICE_NAME = "Microsoft Yaoyao";

        public string NewTittle { get; set; } = "默认标题";
        public string NewContent { get; set; } = "默认内容";
        public string New_extra_text { get; set; } = "";
        public string studentsName { get; set; } = "";
        public bool AutoApplyProperties { get; set; } = true;

        private bool _disposed = false;
        private IntPtr _hWnd;
        public bool TTS_open = Properties.Settings.Default.tts;
        public bool IsMainWindow = Properties.Settings.Default.IsMain;
        public bool SoundEnabled = Properties.Settings.Default.SoundEnabled;

        private SpeechSynthesizer? _synthesizer;

        public Window3()
        {
            InitializeComponent();
            
            // 订阅事件用于清理
            this.Unloaded += OnWindowUnloaded;
            this.Closed += OnWindowClosed;

            if (SoundEnabled && !IsMainWindow)
            {
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch (SystemException ex) // 使用更具体的异常类型
                {
                    Debug.WriteLine($"播放声音失败: {ex.Message}");
             
                }
                this.SourceInitialized += AWindow_SourceInitialized;

                // 订阅会话切换事件（处理锁屏）
                Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            }

            // 初始化语音合成器（非阻塞播放）
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

            if (AutoApplyProperties)
            {
                this.Loaded += OnWindowLoaded;
            }
        }
        private void AWindow_SourceInitialized(object sender, EventArgs e)
        {
            // 获取窗口句柄
            var hwnd = new WindowInteropHelper(this).Handle;

            // 设置窗口扩展样式为工具窗口
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // 设置窗口为系统级置顶
            SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            // 同时设置WPF的Topmost属性作为辅助
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
                // 解锁时恢复置顶
                case Microsoft.Win32.SessionSwitchReason.SessionUnlock:
                    this.Topmost = true;
                    // 重新设置系统级置顶
                    var hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    }
                    break;
            }
        }

        // 窗口激活状态变化时重新确保置顶
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // 确保窗口保持置顶状态
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            base.OnClosed(e);
        }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyPropertiesToUI();
        }

        public void ApplyPropertiesToUI()
        {
            try
            {
                // 安全地设置标题
                if (test_tittle != null)
                {
                    test_tittle.Text = NewTittle ?? "默认标题";
                }

                // 安全地设置内容
                if (Context != null)
                {
                    Context.Text = NewContent ?? "默认内容";
                }

                // 安全地设置额外文本
                if (extra_text != null)
                {
                    extra_text.Text = New_extra_text ?? "";
                }

                // 计算文本所需高度并调整窗口
                AdjustWindowHeight();
            }
            catch (SystemException ex)
            {
                Debug.WriteLine($"应用属性到UI失败: {ex.Message}");
                // 确保窗口至少能显示
                this.Height = DEFAULT_WINDOW_HEIGHT;
            }

            // 使用异步播放，避免阻塞 UI 线程
            try
            {
                if (!string.IsNullOrWhiteSpace(NewContent) && _synthesizer != null && TTS_open)
                {
                    _synthesizer.Rate = VOICE_RATE;
                    
                    // 安全地选择语音
                    var availableVoices = _synthesizer.GetInstalledVoices();
                    bool voiceFound = false;
                    foreach (var voice in availableVoices)
                    {
                        if (voice.VoiceInfo.Name.Contains(DEFAULT_VOICE_NAME))
                        {
                            _synthesizer.SelectVoice(voice.VoiceInfo.Name);
                            voiceFound = true;
                            break;
                        }
                    }
                    
                    // 如果没有找到指定语音，使用默认语音
                    if (!voiceFound)
                    {
                        Debug.WriteLine($"未找到语音 {DEFAULT_VOICE_NAME}，使用默认语音");
                    }

                    // 取消任何未完成的异步播放，避免重叠
                    _synthesizer.SpeakAsyncCancelAll();
                    _synthesizer.SpeakAsync(studentsName);
                    
                    foreach (var voice in _synthesizer.GetInstalledVoices())
                    {
                        Debug.WriteLine($"语音名称: {voice.VoiceInfo.Name}");
                    }
                }
            }
            catch (SystemException ex)
            {
                Debug.WriteLine($"语音播放失败: {ex.Message}");
            }
        }

        private void AdjustWindowHeight()
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(new Action(AdjustWindowHeight));
                    return;
                }

                // 测量文本所需高度
                double contentHeight = 0;
                double extraHeight = 0;

                if (Context != null && Context.IsMeasureValid)
                {
                    Context.Measure(new Size(TEXT_WIDTH, double.PositiveInfinity));
                    contentHeight = Context.DesiredSize.Height;
                }

                if (extra_text != null && extra_text.IsMeasureValid)
                {
                    extra_text.Measure(new Size(TEXT_WIDTH, double.PositiveInfinity));
                    extraHeight = extra_text.DesiredSize.Height;
                }

                // 计算内容区域所需高度
                double totalContentHeight = TITLE_HEIGHT + contentHeight + extraHeight + 40;

                // 确保最小高度
                totalContentHeight = Math.Max(totalContentHeight, MIN_WINDOW_HEIGHT);

                // 设置窗口高度 = 内容高度 + 底部按钮区域高度
                this.Height = totalContentHeight + BOTTOM_BUTTON_HEIGHT;
            }
            catch (SystemException ex)
            {
                Debug.WriteLine($"调整窗口高度失败: {ex.Message}");
                // 使用默认高度
                this.Height = DEFAULT_WINDOW_HEIGHT;
            }
        }

        void OK(object sender, RoutedEventArgs e)
        {
            // 清理数据绑定
            this.DataContext = null;

            // 关闭窗口
            this.Close();
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            // 移除所有事件处理程序
            this.Unloaded -= OnWindowUnloaded;
            this.Closed -= OnWindowClosed;
            this.Loaded -= OnWindowLoaded;

            // 清理窗口内容
            this.Content = null;

            // 调用Dispose释放资源
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    try
                    {
                        // 清理UI元素引用
                        test_tittle = null;
                        Context = null;
                        extra_text = null;

                        // 清理可能的子控件
                        this.Content = null;

                        // 停止并释放语音合成器
                        try
                        {
                            if (_synthesizer != null)
                            {
                                _synthesizer.SpeakAsyncCancelAll();
                                _synthesizer.Dispose();
                                _synthesizer = null;
                            }
                        }
                        catch (SystemException ex)
                        {
                            Debug.WriteLine($"释放 SpeechSynthesizer 失败: {ex.Message}");
                        }
                    }
                    catch (SystemException ex)
                    {
                        Debug.WriteLine($"释放托管资源失败: {ex.Message}");
                    }
                }

                // 释放非托管资源
                _hWnd = IntPtr.Zero;

                _disposed = true;
            }
        }

        ~Window3()
        {
            Dispose(false);
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);
    }
}
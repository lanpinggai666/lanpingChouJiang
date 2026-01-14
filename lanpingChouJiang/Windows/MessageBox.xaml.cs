using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Wpf.Ui.Controls;
using System.Speech.Synthesis;

namespace lanpingcj
{
    public partial class MessageBox : Window
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
        
        private const int VOICE_RATE = 0; // 语音速率，0为正常速度
        private const string DEFAULT_VOICE_NAME = "Microsoft Yaoyao";

        public string NewTittle { get; set; } 
        public string NewContent { get; set; }
        public string New_extra_text { get; set; } 
        public string studentsName { get; set; }
        public bool AutoApplyProperties { get; set; } = true;

        public bool TTS_open = Properties.Settings.Default.tts;
        public bool IsMainWindow = Properties.Settings.Default.IsMain;
        public bool SoundEnabled = Properties.Settings.Default.SoundEnabled;

        private SpeechSynthesizer? _synthesizer;

        public MessageBox()
        {
            InitializeComponent();


            if (AutoApplyProperties)
            {
                this.Loaded += (s, e) => ApplyPropertiesToUI();
            }




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
                SourceInitialized += AWindow_SourceInitialized;

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



        public void ApplyPropertiesToUI()
        {

            extra_text.Text = New_extra_text;
            test_tittle.Text = NewTittle;
            Context.Text = NewContent;
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

    

        void OK(object sender, RoutedEventArgs e)
        {
            

            // 关闭窗口
            this.Close();
        }

       

      

        
            
        


      
    }
}
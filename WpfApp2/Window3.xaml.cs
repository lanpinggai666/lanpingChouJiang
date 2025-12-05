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
        public string NewTittle { get; set; } = "默认标题";
        public string NewContent { get; set; } = "默认内容";
        public string New_extra_text { get; set; } = "";
        public string studentsName { get; set; } = "";
        public bool AutoApplyProperties { get; set; } = true;
        //public bool IsWindow1 { get; set; }

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

            if (SoundEnabled == true && IsMainWindow == false)
            {
                try
                {
                    
                    
                        SystemSounds.Asterisk.Play();
                    
                        
                    
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"播放声音失败: {ex.Message}");
                }
            }

            // 设置窗口圆角
            try
            {
                _hWnd = new WindowInteropHelper(this).EnsureHandle();
                var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
                var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(_hWnd, attribute, ref preference, sizeof(uint));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置窗口圆角失败: {ex.Message}");
            }

            // 初始化语音合成器（非阻塞播放）
            try
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SetOutputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化 SpeechSynthesizer 失败: {ex.Message}");
                _synthesizer = null;
            }

            if (AutoApplyProperties)
            {
                this.Loaded += OnWindowLoaded;
            }

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
            catch (Exception ex)
            {
                Debug.WriteLine($"应用属性到UI失败: {ex.Message}");
                // 确保窗口至少能显示
                this.Height = 250;
            }

            // 使用异步播放，避免阻塞 UI 线程
            try
            {
                if (!string.IsNullOrWhiteSpace(NewContent) && _synthesizer != null && TTS_open == true)
                {
                    // 取消任何未完成的异步播放，避免重叠
                    _synthesizer.SpeakAsyncCancelAll();
                    _synthesizer.SpeakAsync(studentsName);
                }
            }
            catch (Exception ex)
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
                double titleHeight = 75; // 标题区域固定75像素
                double contentHeight = 0;
                double extraHeight = 0;

                if (Context != null && Context.IsMeasureValid)
                {
                    Context.Measure(new Size(290, double.PositiveInfinity));
                    contentHeight = Context.DesiredSize.Height;
                }

                if (extra_text != null && extra_text.IsMeasureValid)
                {
                    extra_text.Measure(new Size(290, double.PositiveInfinity));
                    extraHeight = extra_text.DesiredSize.Height;
                }

                // 计算内容区域所需高度
                double totalContentHeight = titleHeight + contentHeight + extraHeight + 40;

                // 确保最小高度
                totalContentHeight = Math.Max(totalContentHeight, 150);

                // 设置窗口高度 = 内容高度 + 底部按钮区域高度(60)
                this.Height = totalContentHeight + 60;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"调整窗口高度失败: {ex.Message}");
                // 使用默认高度
                this.Height = 250;
            }

        }

        void OK(object sender, RoutedEventArgs e)
        {
            // 清理事件处理程序


            // 清理数据绑定
            this.DataContext = null;

            // 关闭窗口
            this.Close();
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
        }
        // 清理UI元素的数据绑定


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
                        // 清理属性引用
                        NewTittle = null;
                        NewContent = null;
                        New_extra_text = null;

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
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"释放 SpeechSynthesizer 失败: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
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
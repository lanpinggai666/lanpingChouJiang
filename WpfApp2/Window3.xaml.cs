using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WpfApp2
{
    public partial class Window3 : Window
    {
        public string NewTittle { get; set; } = "默认标题";
        public string NewContent { get; set; } = "默认内容";
        public string New_extra_text { get; set; } = "";
        public bool AutoApplyProperties { get; set; } = true;

        public Window3()
        {
            InitializeComponent();
            if (AppSettings.SoundEnabled)
            {
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"播放声音失败: {ex.Message}");
                }
            }           // 设置窗口圆角
            try
            {
                IntPtr hWnd = new WindowInteropHelper(this).EnsureHandle();
                var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
                var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(hWnd, attribute, ref preference, sizeof(uint));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置窗口圆角失败: {ex.Message}");
            }

            if (AutoApplyProperties)
            {
                this.Loaded += (s, e) => ApplyPropertiesToUI();
            }
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
                System.Diagnostics.Debug.WriteLine($"应用属性到UI失败: {ex.Message}");
                // 确保窗口至少能显示
                this.Height = 250;
            }
        }

        private void AdjustWindowHeight()
        {
            try
            {
                // 测量文本所需高度
                double titleHeight = 75; // 标题区域固定75像素
                double contentHeight = 0;
                double extraHeight = 0;

                if (Context != null)
                {
                    Context.Measure(new Size(290, double.PositiveInfinity));
                    contentHeight = Context.DesiredSize.Height;
                }

                if (extra_text != null)
                {
                    extra_text.Measure(new Size(290, double.PositiveInfinity));
                    extraHeight = extra_text.DesiredSize.Height;
                }

                // 计算内容区域所需高度
                double totalContentHeight = titleHeight + contentHeight + extraHeight + 40; // 加上一些边距

                // 确保最小高度
                totalContentHeight = System.Math.Max(totalContentHeight, 150);

                // 设置窗口高度 = 内容高度 + 底部按钮区域高度(60)
                this.Height = totalContentHeight + 60;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调整窗口高度失败: {ex.Message}");
                // 使用默认高度
                this.Height = 250;
            }
        }

        void OK(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        // 定义窗口圆角偏好
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        // 导入DWM API
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);

    }
}
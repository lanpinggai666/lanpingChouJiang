using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace lanpingcj
{
    public partial class WarningMeassageBox : Window
    {
        public string errorNewContent { get; set; } = "默认错误内容";
        public bool AutoApplyProperties { get; set; } = true;
        public ImageSource? ErrorIcon { get; set; }

        public WarningMeassageBox()
        {
            InitializeComponent();
            SystemSounds.Exclamation.Play();

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
                if (errorContext != null)
                {
                    errorContext.Text = errorNewContent ?? "默认错误内容";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("errorContext 控件为 null");
                }

                AdjustWindowHeight();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用属性到UI失败: {ex.Message}");
                this.Height = 250;
            }
        }

        private void AdjustWindowHeight()
        {
            try
            {
                double titleHeight = 75;
                double contentHeight = 0;

                if (errorContext != null)
                {
                    errorContext.Measure(new Size(250, double.PositiveInfinity));
                    contentHeight = errorContext.DesiredSize.Height + 40;
                }
                else
                {
                    contentHeight = 100;
                }

                double totalContentHeight = titleHeight + contentHeight;
                totalContentHeight = System.Math.Max(totalContentHeight, 150);
                this.Height = totalContentHeight + 60;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调整窗口高度失败: {ex.Message}");
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
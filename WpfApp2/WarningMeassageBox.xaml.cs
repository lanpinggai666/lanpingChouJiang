using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp2
{
    /// <summary>
    /// Window3.xaml 的交互逻辑
    /// </summary>
    public partial class WarningMeassageBox : Window
    {
        public string errorNewContent { get; set; } = "默认错误内容";
        public bool AutoApplyProperties { get; set; } = true;
        public ImageSource ErrorIcon { get; set; }

        public WarningMeassageBox()
        {
            InitializeComponent(); // 必须调用这个来初始化XAML中定义的控件
            SystemSounds.Exclamation.Play();

            try
            {
                // 设置窗口圆角
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
                // 安全地设置错误内容
                if (errorContext != null)
                {
                    errorContext.Text = errorNewContent ?? "默认错误内容";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("errorContext 控件为 null");
                }

                // 安全地设置图标
               // if (errorIcon != null && ErrorIcon != null)
               // {
                   // errorIcon.Source = new BitmapImage(new Uri("pack://application:,,,/WpfApp2;component/imageres_1_98.ico"));
                //}
                //else if (errorIcon != null)
               // {
                    // 如果未设置自定义图标，尝试使用默认图标
                  //  TryLoadDefaultIcon();
              //  }

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

        private void TryLoadDefaultIcon()
        {
            
                try
                {
                    // 方法2: 使用WPF UI框架中的图标
                    errorIcon.Source = FindResource("ErrorIcon") as ImageSource;
                }
                catch
                {
                    // 方法3: 隐藏图标
                    errorIcon.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("无法加载错误图标，已隐藏图标");
                }
            }
        

        private void AdjustWindowHeight()
        {
            try
            {
                double titleHeight = 75; // 标题区域固定75像素
                double contentHeight = 0;

                if (errorContext != null)
                {
                    // 测量文本所需高度
                    errorContext.Measure(new Size(250, double.PositiveInfinity)); // 使用最大宽度250
                    contentHeight = errorContext.DesiredSize.Height + 40; // 加上一些边距
                }
                else
                {
                    contentHeight = 100; // 默认内容高度
                }

                // 计算内容区域所需高度
                double totalContentHeight = titleHeight + contentHeight;

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
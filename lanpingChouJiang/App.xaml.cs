using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Windows;

namespace lanpingcj
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 处理系统通知的点击动作
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                if (!args.TryGetValue("action", out string action)) return;

                switch (action)
                {
                    case "RunApp":
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "latest.exe",
                            UseShellExecute = true
                        });
                        break;

                    case "OpenMoreInfo":
                        // 点击"有新版本"通知：打开更新页面
                        Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            var moreInfo = new MoreInfo { ToUpdatePage = true };
                            moreInfo.Show();
                        }));
                        break;
                }
            };
        }
    }
}

using lanpingcj.Views.Pages;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

namespace lanpingcj
{
    public partial class MoreInfo : FluentWindow
    {
        public bool ToUpdatePage { get; set; } = false;

        private bool _unlockChecked = false;

        public async Task CheckUpdate()
        {
            try
            {
                var (latest, mandatory) = await UpdateService.GetLatestVersionAsync();
                if (latest <= UpdateService.CurrentVersion) return;

                var dialog = new ContentDialog(RootContentDialogPresenter);
                string message = mandatory ? "\n这是一个强制更新，我们在当前版本发现了一个严重的Bug，为了您良好的体验请立即更新" : "";

                dialog.Title = "有新版本可用!               ";
                dialog.Content = $"当前版本：{UpdateService.CurrentVersion}\n最新版本：{latest}\n{message}\n如果您想查看更新日志，请访问Github Release页面。";
                dialog.PrimaryButtonText = "确定";
                dialog.CloseButtonText = "关闭";
                dialog.DialogWidth = 400;
                dialog.PrimaryButtonAppearance = ControlAppearance.Primary;
                dialog.SecondaryButtonAppearance = ControlAppearance.Secondary;

                var dialogResult = await dialog.ShowAsync();
                switch (dialogResult)
                {
                    case ContentDialogResult.Primary:
                        ToUpdate();
                        break;
                    case ContentDialogResult.None:
                        if (mandatory)
                        {
                            Process.GetCurrentProcess().Kill();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查更新失败: {ex.Message}");
            }
        }

        public void ToUpdate()
        {
            NavigationView.Navigate(typeof(UpdatePage));
        }

        // 设置锁：开启锁定且设有密码时，必须先输入正确密码才能使用本窗口
        private async Task<bool> EnsureUnlockedAsync()
        {
            var cfg = ConfigService.LoadCurrent();
            if (!cfg.Lock || string.IsNullOrEmpty(cfg.Lock_Password)) return true;

            var passwordBox = new Wpf.Ui.Controls.PasswordBox
            {
                PlaceholderText = "请输入设置密码",
                MinWidth = 260
            };

            var dialog = new ContentDialog(RootContentDialogPresenter)
            {
                Title = "设置已锁定",
                Content = passwordBox,
                PrimaryButtonText = "解锁",
                CloseButtonText = "取消",
                PrimaryButtonAppearance = ControlAppearance.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return false;

            if (passwordBox.Password != cfg.Lock_Password)
            {
                var wrongDialog = new ContentDialog(RootContentDialogPresenter)
                {
                    Title = "密码错误",
                    Content = "密码不正确，窗口即将关闭。",
                    CloseButtonText = "确定"
                };
                await wrongDialog.ShowAsync();
                return false;
            }

            return true;
        }

        public MoreInfo()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (!ToUpdatePage)
                    {
                        NavigationView.Navigate(typeof(SettingsPage));
                    }
                    else
                    {
                        NavigationView.Navigate(typeof(UpdatePage));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"导航失败: {ex.Message}");
                }
            }));

            this.Loaded += async (sender, e) =>
            {
                if (!_unlockChecked)
                {
                    _unlockChecked = true;
                    if (!await EnsureUnlockedAsync())
                    {
                        Close();
                        return;
                    }
                }
                await CheckUpdate();
            };
        }

        public static void ShowUnique()
        {
            var existing = Application.Current.Windows.OfType<MoreInfo>().FirstOrDefault();
            if (existing != null)
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;
                existing.Activate();
                return;
            }

            var more = new MoreInfo();
            more.Show();
        }
    }
}

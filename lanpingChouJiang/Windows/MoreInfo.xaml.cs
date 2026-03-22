using lanpingcj.Views.Pages;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation.Collections;
using Wpf.Ui.Controls;

namespace lanpingcj
{
    public partial class MoreInfo : FluentWindow
    {
        public bool ToUpdatePage { get; set; } = false;

        public static bool IsBelowWindows10()
        {
            try
            {
                const string registryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
                using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        var currentBuild = key.GetValue("CurrentBuildNumber") as string;
                        var productName = key.GetValue("ProductName") as string;

                        if (!string.IsNullOrEmpty(currentBuild) && int.TryParse(currentBuild, out int buildNumber))
                        {
                            return buildNumber < 10240;
                        }

                        if (!string.IsNullOrEmpty(productName))
                        {
                            return !productName.Contains("Windows 10") &&
                                   !productName.Contains("Windows 11");
                        }
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public async Task<(string Version, string Mandatory)> GetVersion()
        {
            try
            {
                string url = "https://update.choujiang.lanpinggai.top/version";
                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                string content = await client.GetStringAsync(url);
                using StringReader reader = new StringReader(content);
                string version = reader.ReadLine()?.Trim() ?? "0.0.0";
                string mandatory = reader.ReadLine()?.Trim() ?? "false";

                return (version, mandatory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return ("0.0.0", "false");
            }
        }

        public async Task CheckUpdate()
        {
            var result = await GetVersion();
            if (!bool.TryParse(result.Mandatory, out bool mandatory))
            {
                mandatory = false;
            }

            Version latestVersion = new Version(!string.IsNullOrEmpty(result.Version) ? result.Version : "0.0.0");
            string currentVerStr = Properties.Settings.Default.ThisVersion;
            Version thisVersion = new Version(!string.IsNullOrEmpty(currentVerStr) ? currentVerStr : "0.0.0");

            if (latestVersion > thisVersion)
            {
                var dialog = new ContentDialog(RootContentDialogPresenter);
                string message = mandatory ? "\n这是一个强制更新，我们在当前版本发现了一个严重的Bug，为了您良好的体验请立即更新" : "";

                dialog.Title = "有新版本可用!               ";
                dialog.Content = $"当前版本：{thisVersion}\n最新版本：{latestVersion}\n{message}\n如果您想查看更新日志，请访问Github Release页面。";
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
        }

        public void ToUpdate()
        {
            NavigationView.Navigate(typeof(UpdatePage));
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;
            while (Math.Round(dblBytes / 1024) >= 1 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }
            return string.Format("{0:0.##} {1}", dblBytes, suffixes[i]);
        }

        public async Task DownloadUpdate()
        {
            string downloadUrl = "https://lanpinggai66-my.sharepoint.com/personal/lanpinggai666_lanpinggai66_onmicrosoft_com/_layouts/52/download.aspx?share=IQDkSqcZUZCtQJOHJJN8yNrpAV2HSnKjGXBBRqOOkY2D4IQ";
            string localFileName = "latest.exe";
            bool isWindows10OrLater = !IsBelowWindows10();

            if (isWindows10OrLater)
            {
                try
                {
                    ShowSimpleToast("开始下载更新", "正在连接服务器...", false, "Download");
                    using (HttpClient client = new HttpClient())
                    {
                        var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                        long? totalBytes = response.Content.Headers.ContentLength;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = System.IO.File.Create(localFileName))
                        {
                            byte[] buffer = new byte[8192];
                            long totalBytesRead = 0;
                            int bytesRead;
                            int lastPercentage = -1;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalBytesRead += bytesRead;

                                if (totalBytes.HasValue)
                                {
                                    int percentage = (int)((double)totalBytesRead / totalBytes.Value * 100);
                                    if (percentage % 5 == 0 && percentage != lastPercentage)
                                    {
                                        lastPercentage = percentage;
                                        UpdateSimpleToast(
                                            $"下载中... {percentage}%",
                                            $"{FormatFileSize(totalBytesRead)} / {FormatFileSize(totalBytes.Value)}"
                                        );
                                    }
                                }
                            }
                        }
                    }
                    ShowSimpleToast("下载完成", "点击安装更新", true, "RunApp");
                }
                catch (Exception ex)
                {
                    ShowSimpleToast("下载失败", ex.Message, true, "error");
                }
            }
            else
            {
                try
                {
                    System.Windows.MessageBox.Show("正在下载文件...不要关闭本窗口！");
                    using (HttpClient client = new HttpClient())
                    {
                        byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);
                        await System.IO.File.WriteAllBytesAsync(localFileName, fileBytes);
                        System.Windows.MessageBox.Show("下载完成！");
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = localFileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"错误: {ex.Message}");
                }
            }
        }

        private void ShowSimpleToast(string title, string message, bool withSound, string toastAction)
        {
            var builder = new ToastContentBuilder()
               .AddArgument("action", toastAction ?? "default")
               .SetBackgroundActivation()
               .AddText(title ?? string.Empty)
               .AddText(message ?? string.Empty);

            builder.Show();
        }

        private void UpdateSimpleToast(string title, string message)
        {
            try
            {
                var builder = new ToastContentBuilder()
                    .AddText(title ?? string.Empty)
                    .AddText(message ?? string.Empty)
                    .AddAudio(new ToastAudio() { Silent = true });

                builder.Show(toast =>
                {
                    toast.Tag = "downloadProgress";
                    toast.Group = "updates";
                });
            }
            catch { }
        }

        public async Task TopDialog()
        {
            var dialog = new ContentDialog(RootContentDialogPresenter);
            dialog.Title = "需要重启                ";
            dialog.Content = "你需要重启应用程序来应用更改。\n置于顶层需要管理员权限并关闭UAC，如果你在更改以后打不开本软件，\n请关闭UAC并重启您的电脑或者运行应用程序安装目录下的重置.bat";
            dialog.PrimaryButtonText = "确定";
            dialog.CloseButtonText = "稍后";
            dialog.PrimaryButtonAppearance = ControlAppearance.Primary;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var mainModule = Process.GetCurrentProcess().MainModule;
                if (mainModule?.FileName != null)
                {
                    Process.Start(mainModule.FileName);
                }
                Environment.Exit(0);
            }
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
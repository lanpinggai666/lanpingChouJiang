using lanpingcj.Views.Pages;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Windows;
using Wpf.Ui.Controls;

namespace lanpingcj
{
    public partial class MoreInfo : FluentWindow
    {
        public async Task<(string Version, string Mandatory)> GetVersion()
        {
            string url = "https://gh.jasonzeng.dev/https://raw.githubusercontent.com/lanpinggai666/lanpingChouJiang/master/version";

            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            string content = await client.GetStringAsync(url);
            Debug.WriteLine($"原始内容: {content}");

            using StringReader reader = new StringReader(content);
            string version = reader.ReadLine()?.Trim() ?? string.Empty;
            string mandatory = reader.ReadLine()?.Trim() ?? string.Empty;

            // 一次性返回两个值
            return (version, mandatory);
        }

        // 调用时接收两个值
        public async Task CheckUpdate()
        {

            var result = await GetVersion();



            bool mandatory = bool.Parse(result.Mandatory);//强制更新
            Version LatestVersion = new Version(result.Version);
            Version ThisVersion = new Version(Properties.Settings.Default.ThisVersion);
            Debug.WriteLine($"当前版本: {ThisVersion}, 最新版本: {LatestVersion}, 强制更新: {mandatory}");
            string message = "";
            if (mandatory)
            {
                 message = "\n这是一个强制更新，我们在当前版本发现了一个严重的Bug，为了您良好的体验请立即更新";
                
            }
            if (LatestVersion > ThisVersion)
            {
                var Dialog = new ContentDialog(RootContentDialogPresenter);

                Dialog.Title = "有新版本可用!                      ";
                Dialog.Content = $"当前版本：{ThisVersion}\n最新版本：{LatestVersion}\n{message}";
                Dialog.PrimaryButtonText = "确定";
                Dialog.CloseButtonText = "关闭";
                Dialog.DialogWidth = 400;
                Dialog.PrimaryButtonAppearance = ControlAppearance.Primary;
                Dialog.SecondaryButtonAppearance = ControlAppearance.Secondary;
                var Dialogresult = await Dialog.ShowAsync();
                switch (Dialogresult)
                {
                    case ContentDialogResult.Primary:
                        await DownloadUpdate();
                        break;
                    case ContentDialogResult.None:
                        // 用户点击了关闭按钮或按ESC
                        if (mandatory)
                        {
                            Process.GetCurrentProcess().Kill();

                        }
                        break;
                }

            }




        }
        public async Task DownloadUpdate()
        {
            string downloadUrl = "https://lanpinggai66-my.sharepoint.com/personal/lanpinggai666_lanpinggai66_onmicrosoft_com/_layouts/52/download.aspx?share=IQDkSqcZUZCtQJOHJJN8yNrpAV2HSnKjGXBBRqOOkY2D4IQ";
            string localFileName = "latest.exe";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    Debug.WriteLine("正在下载文件...");

                    // 异步下载文件
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);

                    // 保存文件
                    await File.WriteAllBytesAsync(localFileName, fileBytes);
                    Console.WriteLine("下载完成！");
                }

                // 执行文件
                Process.Start(new ProcessStartInfo
                {
                    FileName = localFileName,
                    UseShellExecute = true  // 使用系统外壳执行
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }
        public MoreInfo()
        {
            InitializeComponent();

            // 订阅 Loaded 事件，确保 UI 完全加载后再进行导航
            // Loaded += OnWindowLoaded;
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    NavigationView.Navigate(typeof(SettingsPage));
                }
                catch (System.Exception ex)
                {
                    // 如果导航失败，显示错误信息
                    //Wpf.Ui.Controls.MessageBox.Show($"导航失败: {ex.Message}", "错误", Wpf.Ui.Controls.MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"导航失败: {ex.Message}");
                }

            });
            this.Loaded += async (sender, e) =>
            {


                await CheckUpdate();
            };

        }

        //
    }
}
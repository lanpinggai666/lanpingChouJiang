using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace lanpingcj.Views.Pages
{
    /// <summary>
    /// UpdatePage.xaml 的交互逻辑
    /// </summary>
    public partial class UpdatePage : Page
    {
        private const double DefaultInterval = 20.0;
        string localFileName = "latest.exe";
        public bool FromOther { get; set;} = false;


        public UpdatePage()
        {
            InitializeComponent();
            LoadSettings();
            CheckFormOther();
        }
        private void CheckFormOther()
        { 
           if(FromOther)
            {
                HaveUpdateText();
            }
        }
        public async Task DownloadUpdate()
        {
            string downloadUrl = "https://lanpinggai66-my.sharepoint.com/personal/lanpinggai666_lanpinggai66_onmicrosoft_com/_layouts/52/download.aspx?share=IQDkSqcZUZCtQJOHJJN8yNrpAV2HSnKjGXBBRqOOkY2D4IQ";
       
                try
                {
                    {
                        // 显示开始Toast

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

                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalBytesRead += bytesRead;

                                    if (totalBytes.HasValue)
                                    {
                                        int percentage = (int)((double)totalBytesRead / totalBytes.Value * 100);

                                        // 每5%更新一次Toast，避免过于频繁
                                        if (percentage % 5 == 0)
                                        {
                                           InstallButton.Content = $"{percentage}%";
                                           InstallButton.IsEnabled = false;
                                    }
                                    }
                                }
                            }
                        }


                        // 显示完成Toast
                        ShowSimpleToast("下载完成", "点击安装更新", true, "RunApp");
                    InstallButton.Content = "安装";
                    InstallButton.IsEnabled = true;
                    InstallButton.Click += (sender, e) =>
                    {
                        InstallApp();
                    };

                        // 执行文件
                    }

                }
                catch (Exception ex)
                {
                    ShowSimpleToast("下载失败", ex.Message, true, "error");
                }
            }
        private void ShowSimpleToast(string title, string message, bool withSound, string ToastAction)
        {
            var builder = new ToastContentBuilder()
               .AddArgument("action", ToastAction)
               .SetBackgroundActivation()
                .AddText(title)
                .AddText(message);



            builder.Show();
        }
        private void InstallApp()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = localFileName,
                UseShellExecute = true  // 使用系统外壳执行
            });
        }
        private void LoadSettings()
        {
            double savedTime = Properties.Settings.Default.Updatetime;
            UpdateIntervalBox.Value = savedTime > 0 ? savedTime : DefaultInterval;
        }
        //检查更新的按钮。。。
        private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
        {
            CheckingText();
            CheckUpdateButton.IsEnabled = false;

            try
            {
                await CheckForUpdates();
            }
            catch (Exception ex)
            {   
                Updateerror();
                UpdateVersionCard.Visibility = Visibility.Visible;
                VereionText.Text = $"错误：{ex.Message}，请检查Internet连接和对Github的连通性！";
            }
            CheckUpdateButton.IsEnabled = true;
        }
        //获取版本信息
        public async Task<string> GetVersion()
        {
           
                string url = "https://raw.githubusercontent.com/lanpinggai666/lanpingChouJiang/master/version";

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                string content = await client.GetStringAsync(url);
                Debug.WriteLine($"原始内容: {content}");

                using StringReader reader = new StringReader(content);
                string version = reader.ReadLine()?.Trim() ?? string.Empty;
                string mandatory = reader.ReadLine()?.Trim() ?? string.Empty;


            if (version == null || version == "")
            {
                return "0.0.0";
            }
            else
            {
                return version;
            }
            
        }
        private async  Task CheckForUpdates()
        {

               string Version =  await GetVersion();
            
            
            
            Version LatestVersion = new Version(Version);
            Version ThisVersion = new Version(Properties.Settings.Default.ThisVersion);
            if (LatestVersion > ThisVersion)
            { 
                UpdateVersionCard.Visibility = Visibility.Visible;
                VereionText.Text = Version;
                HaveUpdateText();
            }
            else
            {
                UpdateText();
            }

        }
        private void UpdateIntervalBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (UpdateIntervalBox == null) return;

            double? newValue = UpdateIntervalBox.Value;

            if (newValue == null || newValue <= 0)
            {
                UpdateIntervalBox.Value = DefaultInterval;
                SaveSetting(DefaultInterval);
            }
            else
            {
                SaveSetting(newValue.Value);
            }
        }

        private void SaveSetting(double value)
        {
            // 保存到 Properties.Settings.Default 
            Properties.Settings.Default.Updatetime = value;
            Properties.Settings.Default.Save();
        }
        private void CheckingText()
        {
            StateText.Text = "正在检查更新...";
            LastCheckTimeText.Visibility = Visibility.Hidden;
            StateIcon.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Visible;
            Task Check = CheckForUpdates();
        }
        private void UpdateText()
        {
            StateText.Text = "你使用的是最新版本";
            LastCheckTimeText.Visibility = Visibility.Visible;
            StateIcon.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Hidden;
        }
        private void HaveUpdateText()
        {
            StateText.Text = "更新可用";
            LastCheckTimeText.Visibility = Visibility.Visible;
            StateIcon.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;
            CheckUpdateButton.Content = "下载";
            CheckUpdateButton.Click += Install;
            CheckUpdateButton.IsEnabled = true;
        }
        private void Updateerror()
        {
            StateText.Text = "更新出错";
            StateIcon.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;
            CheckUpdateButton.Content = "重试";
            StateIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.ErrorCircle24;
            BrushConverter brushConverter = new BrushConverter();
            Brush? brush = (Brush)brushConverter.ConvertFromString("#E53935");
            StateIcon.Foreground = brush ;


        }
        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private async void Install(object sender, RoutedEventArgs e)
        {
            CheckUpdateButton.IsEnabled = false;
            await DownloadUpdate();
        }
    }
}


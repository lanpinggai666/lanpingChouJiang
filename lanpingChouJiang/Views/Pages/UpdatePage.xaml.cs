using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace lanpingcj.Views.Pages
{
    public partial class UpdatePage : Page
    {
        private const double DefaultInterval = 20.0;
        private readonly string localFileName = "latest.exe";
        public bool FromOther { get; set; } = false;
        private bool isDownloading = false;
        private CancellationTokenSource? downloadCancellation;

        public UpdatePage()
        {
            InitializeComponent();
            LoadSettings();
            CheckFormOther();
            RestoreDownloadState();
            this.Unloaded += UpdatePage_Unloaded;
        }

        private void UpdatePage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (isDownloading)
            {
                downloadCancellation?.Cancel();
            }
        }

        private void CheckFormOther()
        {
            if (FromOther)
            {
                HaveUpdateText();
            }
        }

        private void RestoreDownloadState()
        {
            bool wasDownloading = Properties.Settings.Default.IsDownloading;
            int lastProgress = Properties.Settings.Default.DownloadProgress;

            if (wasDownloading && lastProgress > 0)
            {
                if (File.Exists(localFileName))
                {
                    FileInfo fileInfo = new FileInfo(localFileName);
                    long totalSize = Properties.Settings.Default.DownloadTotalSize;

                    if (lastProgress >= 100 || (totalSize > 0 && fileInfo.Length >= totalSize))
                    {
                        ShowDownloadComplete();
                    }
                    else
                    {
                        InstallButton.Content = $"{lastProgress}%";
                        InstallButton.IsEnabled = false;
                        CheckUpdateButton.Content = "继续下载";
                        CheckUpdateButton.IsEnabled = true;
                        ResetButtonClickEvents();
                        CheckUpdateButton.Click += Install;
                        StateText.Text = $"下载已暂停 ({lastProgress}%)";
                    }
                }
                else
                {
                    ResetDownloadState();
                }
            }
        }

        private void ResetDownloadState()
        {
            Properties.Settings.Default.IsDownloading = false;
            Properties.Settings.Default.DownloadProgress = 0;
            Properties.Settings.Default.DownloadTotalSize = 0;
            Properties.Settings.Default.Save();
        }

        public async Task DownloadUpdate()
        {
            string downloadUrl = "https://lanpinggai66-my.sharepoint.com/personal/lanpinggai666_lanpinggai66_onmicrosoft_com/_layouts/52/download.aspx?share=IQDkSqcZUZCtQJOHJJN8yNrpAV2HSnKjGXBBRqOOkY2D4IQ";

            if (isDownloading) return;

            isDownloading = true;
            CheckUpdateButton.IsEnabled = false;
            InstallButton.IsEnabled = false;
            downloadCancellation = new CancellationTokenSource();

            Properties.Settings.Default.IsDownloading = true;
            Properties.Settings.Default.Save();

            StateText.Text = "正在下载...";
            ProgressBar.Visibility = Visibility.Visible;

            try
            {
                long existingFileSize = 0;
                if (File.Exists(localFileName))
                {
                    existingFileSize = new FileInfo(localFileName).Length;
                }

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(30);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                if (existingFileSize > 0)
                {
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingFileSize, null);
                }

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, downloadCancellation.Token);
                bool supportsResume = response.StatusCode == System.Net.HttpStatusCode.PartialContent;

                if (!supportsResume && existingFileSize > 0)
                {
                    try { File.Delete(localFileName); } catch { }
                    existingFileSize = 0;
                    request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, downloadCancellation.Token);
                }

                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                long actualTotalBytes = (totalBytes ?? 0) + (supportsResume ? existingFileSize : 0);

                Properties.Settings.Default.DownloadTotalSize = actualTotalBytes;
                Properties.Settings.Default.Save();

                FileMode fileMode = (supportsResume && existingFileSize > 0) ? FileMode.Append : FileMode.Create;

                using (var stream = await response.Content.ReadAsStreamAsync(downloadCancellation.Token))
                using (var fileStream = new FileStream(localFileName, fileMode, FileAccess.Write, FileShare.None, 8192, true))
                {
                    byte[] buffer = new byte[8192];
                    long totalBytesRead = existingFileSize;
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, downloadCancellation.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, downloadCancellation.Token);
                        totalBytesRead += bytesRead;

                        if (actualTotalBytes > 0)
                        {
                            int percentage = (int)((double)totalBytesRead / actualTotalBytes * 100);
                            ProgressBar.Visibility = Visibility.Collapsed;
                            UpdateVersionCard.Visibility = Visibility.Visible;
                            InstallButton.Content = $"{percentage}%";
                            CheckUpdateButton.Content = "全部安装";
                            StateText.Text = "更新可用";

                            Properties.Settings.Default.DownloadProgress = Math.Min(percentage, 99);
                            Properties.Settings.Default.Save();
                        }
                    }
                }

                ShowDownloadComplete();
                ShowSimpleToast("下载完成", "点击安装更新", true, "RunApp");
            }
            catch (OperationCanceledException)
            {
                StateText.Text = "下载已暂停";
                CheckUpdateButton.Content = "继续下载";
                CheckUpdateButton.IsEnabled = true;
                ResetButtonClickEvents();
                CheckUpdateButton.Click += Install;
                ProgressBar.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                Updateerror(ex.Message);
            }
            finally
            {
                isDownloading = false;
                downloadCancellation?.Dispose();
                downloadCancellation = null;
            }
        }

        private void ResetButtonClickEvents()
        {
            CheckUpdateButton.Click -= OnCheckUpdatesClick;
            CheckUpdateButton.Click -= Install;
            CheckUpdateButton.Click -= CheckUpdateButton_InstallClick;

            InstallButton.Click -= InstallButton_Click;
            InstallButton.Click -= OnCheckUpdatesClick;
            InstallButton.Click -= Install;
            InstallButton.Click -= CheckUpdateButton_InstallClick;
        }

        private void ShowDownloadComplete()
        {
            StateText.Text = "更新可用";
            ProgressBar.Visibility = Visibility.Collapsed;
            InstallButton.Content = "安装";
            InstallButton.IsEnabled = true;

            ResetButtonClickEvents();

            InstallButton.Click += InstallButton_Click;
            CheckUpdateButton.Click += CheckUpdateButton_InstallClick;
            CheckUpdateButton.Content = "全部安装";
            CheckUpdateButton.IsEnabled = true;

            Properties.Settings.Default.IsDownloading = false;
            Properties.Settings.Default.DownloadProgress = 100;
            Properties.Settings.Default.Save();
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e) => InstallApp();
        private void CheckUpdateButton_InstallClick(object sender, RoutedEventArgs e) => InstallApp();

        private void ShowSimpleToast(string title, string message, bool withSound, string ToastAction)
        {
            new ToastContentBuilder()
                .AddArgument("action", ToastAction)
                .SetBackgroundActivation()
                .AddText(title)
                .AddText(message)
                .Show();
        }

        private void InstallApp()
        {
            if (!File.Exists(localFileName))
            {
                ResetDownloadState();
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = localFileName,
                    UseShellExecute = true
                });
                ResetDownloadState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void LoadSettings()
        {
            double savedTime = Properties.Settings.Default.Updatetime;
            UpdateIntervalBox.Value = savedTime > 0 ? savedTime : DefaultInterval;
        }

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
                Updateerror(ex.Message);
                UpdateVersionCard.Visibility = Visibility.Collapsed;
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        public async Task<string> GetVersion()
        {
            try
            {
                string url = "https://update.choujiang.lanpinggai.top/version";
                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                string content = await client.GetStringAsync(url);
                if (string.IsNullOrWhiteSpace(content)) return "0.0.0";

                using StringReader reader = new StringReader(content);
                return (await reader.ReadLineAsync())?.Trim() ?? "0.0.0";
            }
            catch { return "0.0.0"; }
        }

        private async Task CheckForUpdates()
        {
            string versionStr = await GetVersion();
            Version latestVersion = new Version(versionStr);
            string currentVerStr = Properties.Settings.Default.ThisVersion;
            Version thisVersion = new Version(string.IsNullOrWhiteSpace(currentVerStr) ? "0.0.0" : currentVerStr);

            if (latestVersion > thisVersion)
            {
                UpdateVersionCard.Visibility = Visibility.Visible;
                VereionText.Text = versionStr;
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
            double finalValue = (newValue == null || newValue <= 0 || newValue > 180) ? DefaultInterval : newValue.Value;
            UpdateIntervalBox.Value = finalValue;
            Properties.Settings.Default.Updatetime = finalValue;
            Properties.Settings.Default.Save();
        }

        private void CheckingText()
        {
            StateText.Text = "正在检查更新...";
            LastCheckTimeText.Visibility = Visibility.Hidden;
            StateIcon.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Visible;
            ErrorCard.Visibility = Visibility.Collapsed;
            UpdateVersionCard.Visibility = Visibility.Collapsed;
        }

        private void UpdateText()
        {
            StateText.Text = "你使用的是最新版本";
            LastCheckTimeText.Visibility = Visibility.Visible;
            StateIcon.Visibility = Visibility.Visible;
            ErrorCard.Visibility = Visibility.Collapsed;
            ProgressBar.Visibility = Visibility.Hidden;
            StateIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
            StateIcon.Foreground = (Brush?)new BrushConverter().ConvertFromString("#37B24D") ?? Brushes.Green;
            UpdateVersionCard.Visibility = Visibility.Collapsed;
        }

        private void HaveUpdateText()
        {
            StateText.Text = "更新可用";
            LastCheckTimeText.Visibility = Visibility.Visible;
            StateIcon.Visibility = Visibility.Hidden;
            ErrorCard.Visibility = Visibility.Collapsed;
            ProgressBar.Visibility = Visibility.Hidden;
            CheckUpdateButton.Content = "下载";
            ResetButtonClickEvents();
            CheckUpdateButton.Click += Install;
            CheckUpdateButton.IsEnabled = true;
        }

        private void Updateerror(string error)
        {
            StateText.Text = "更新出错";
            CheckUpdateButton.IsEnabled = true;
            StateIcon.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Hidden;
            CheckUpdateButton.Content = "重试";
            ResetButtonClickEvents();
            CheckUpdateButton.Click += OnCheckUpdatesClick;
            StateIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.ErrorCircle24;
            StateIcon.Foreground = (Brush?)new BrushConverter().ConvertFromString("#E53935") ?? Brushes.Red;

            ErrorCard.Visibility = Visibility.Visible;
            errorText.Text = $"更新出错：{error} 请检查网络连接！";
            UpdateVersionCard.Visibility = Visibility.Hidden;
            isDownloading = false;
            Properties.Settings.Default.IsDownloading = false;
            Properties.Settings.Default.Save();
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }

        private async void Install(object sender, RoutedEventArgs e)
        {
            CheckUpdateButton.IsEnabled = false;
            await DownloadUpdate();
        }
    }
}
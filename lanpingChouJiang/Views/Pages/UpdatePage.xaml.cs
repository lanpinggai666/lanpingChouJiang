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
        public bool FromOther { get; set; } = false;
        private bool isDownloading = false;
        private CancellationTokenSource? downloadCancellation;

        public UpdatePage()
        {
            InitializeComponent();
            LoadSettings();
            CheckFormOther();
            RestoreDownloadState();

            // 订阅 Unloaded 事件
            this.Unloaded += UpdatePage_Unloaded;
        }

        // 页面卸载时的处理
        private void UpdatePage_Unloaded(object sender, RoutedEventArgs e)
        {
            // 如果正在下载，取消下载
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

        // 恢复下载状态
        private void RestoreDownloadState()
        {
            bool wasDownloading = Properties.Settings.Default.IsDownloading;
            int lastProgress = Properties.Settings.Default.DownloadProgress;

            if (wasDownloading && lastProgress > 0)
            {
                // 检查文件是否存在
                if (File.Exists(localFileName))
                {
                    FileInfo fileInfo = new FileInfo(localFileName);

                    // 如果文件已完成下载（100%）
                    if (lastProgress >= 100 || fileInfo.Length >= Properties.Settings.Default.DownloadTotalSize)
                    {
                        ShowDownloadComplete();
                    }
                    else
                    {
                        // 显示之前的下载进度
                        InstallButton.Content = $"{lastProgress}%";
                        InstallButton.IsEnabled = false;
                        CheckUpdateButton.Content = "继续下载";
                        CheckUpdateButton.IsEnabled = true;
                        CheckUpdateButton.Click -= OnCheckUpdatesClick; // 移除检查更新事件
                        CheckUpdateButton.Click -= Install; // 移除可能存在的Install事件
                        CheckUpdateButton.Click += Install; // 添加下载事件
                        StateText.Text = $"下载已暂停 ({lastProgress}%)";
                    }
                }
                else
                {
                    // 文件不存在，重置状态
                    ResetDownloadState();
                }
            }
        }

        // 重置下载状态
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

            if (isDownloading)
            {
              //  MessageBox.Show("已有下载任务正在进行中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            isDownloading = true;
            CheckUpdateButton.IsEnabled = false;
            InstallButton.IsEnabled = false;
            downloadCancellation = new CancellationTokenSource();

            // 标记正在下载
            Properties.Settings.Default.IsDownloading = true;
            Properties.Settings.Default.Save();

            StateText.Text = "更新可用";
            ProgressBar.Visibility = Visibility.Visible;

            try
            {
                long existingFileSize = 0;

                // 检查是否支持断点续传
                if (File.Exists(localFileName))
                {
                    FileInfo fileInfo = new FileInfo(localFileName);
                    existingFileSize = fileInfo.Length;
                }

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(30);

                    // 创建请求
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

                    // 如果文件已存在，添加 Range 头实现断点续传
                    if (existingFileSize > 0)
                    {
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingFileSize, null);
                    }

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, downloadCancellation.Token);

                    // 检查是否支持断点续传
                    bool supportsResume = response.StatusCode == System.Net.HttpStatusCode.PartialContent;

                    if (!supportsResume && existingFileSize > 0)
                    {
                        // 服务器不支持断点续传，询问用户是否重新下载
                       

                       

                        // 删除旧文件，重新下载
                        try
                        {
                            File.Delete(localFileName);
                            existingFileSize = 0;
                        }
                        catch (IOException ex)
                        {
                            throw new IOException($"无法删除旧文件: {ex.Message}");
                        }

                        // 重新发送请求
                        request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                        response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, downloadCancellation.Token);
                    }

                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;
                    long actualTotalBytes = totalBytes ?? 0;

                    // 如果是断点续传，计算总大小
                    if (supportsResume)
                    {
                        actualTotalBytes += existingFileSize;
                    }

                    // 保存总大小
                    Properties.Settings.Default.DownloadTotalSize = actualTotalBytes;
                    Properties.Settings.Default.Save();

                    // 使用 Append 模式打开文件（如果支持断点续传）
                    FileMode fileMode = (supportsResume && existingFileSize > 0) ? FileMode.Append : FileMode.Create;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(localFileName, fileMode, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        byte[] buffer = new byte[8192];
                        long totalBytesRead = existingFileSize; // 从已下载的大小开始
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, downloadCancellation.Token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead, downloadCancellation.Token);
                            totalBytesRead += bytesRead;

                            if (actualTotalBytes > 0)
                            {
                                int percentage = (int)((double)totalBytesRead / actualTotalBytes * 100);

                                // 更新进度
                                ProgressBar.Visibility = Visibility.Collapsed;
                                UpdateVersionCard.Visibility = Visibility.Visible;
                                InstallButton.Content = $"{percentage}%";
                                CheckUpdateButton.Content = $"下载中 ";
                                StateText.Text = $"更新可用";

                                // 保存进度
                                Properties.Settings.Default.DownloadProgress = percentage;
                                Properties.Settings.Default.Save();
                            }
                        }
                    }
                }

                // 下载完成
                ShowDownloadComplete();
                ShowSimpleToast("下载完成", "点击安装更新", true, "RunApp");
            }
            catch (OperationCanceledException)
            {
                StateText.Text = "下载已暂停";
                CheckUpdateButton.Content = "继续下载";
                CheckUpdateButton.IsEnabled = true;
                CheckUpdateButton.Click -= OnCheckUpdatesClick;
                CheckUpdateButton.Click -= Install;
                CheckUpdateButton.Click += Install;
                ProgressBar.Visibility = Visibility.Hidden;
            }
            catch (IOException ex)
            {
                Updateerror(ex.Message);
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

        // 显示下载完成状态
        private void ShowDownloadComplete()
        {
            InstallButton.Content = "安装";
            InstallButton.IsEnabled = true;
            InstallButton.Click -= InstallButton_Click;
            InstallButton.Click += InstallButton_Click;

            CheckUpdateButton.Click -= OnCheckUpdatesClick;
            CheckUpdateButton.Click -= Install;
            CheckUpdateButton.Click -= CheckUpdateButton_InstallClick;
            CheckUpdateButton.Click += CheckUpdateButton_InstallClick;
            CheckUpdateButton.Content = "安装";
            CheckUpdateButton.IsEnabled = true;

            // 标记下载完成
            Properties.Settings.Default.IsDownloading = false;
            Properties.Settings.Default.DownloadProgress = 100;
            Properties.Settings.Default.Save();

            StateText.Text = "更新可用";
            ProgressBar.Visibility = Visibility.Hidden;
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallApp();
        }

        private void CheckUpdateButton_InstallClick(object sender, RoutedEventArgs e)
        {
            InstallApp();
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
            if (!File.Exists(localFileName))
            {
               // MessageBox.Show("安装文件不存在，请重新下载", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // 安装后重置下载状态
                ResetDownloadState();
            }
            catch (Exception ex)
            {
               // MessageBox.Show($"启动安装程序失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
            double savedTime = Properties.Settings.Default.Updatetime;
            UpdateIntervalBox.Value = savedTime > 0 ? savedTime : DefaultInterval;
        }

        //检查更新的按钮
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
                UpdateVersionCard.Visibility = Visibility.Visible;
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
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

            if (string.IsNullOrEmpty(version))
            {
                return "0.0.0";
            }
            else
            {
                return version;
            }
        }

        private async Task CheckForUpdates()
        {
            string Version = await GetVersion();

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

            if (newValue == null || newValue <= 0 || newValue > 180)
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
            Properties.Settings.Default.Updatetime = value;
            Properties.Settings.Default.Save();
        }

        private void CheckingText()
        {
            StateText.Text = "正在检查更新...";
            LastCheckTimeText.Visibility = Visibility.Hidden;
            StateIcon.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Visible;
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
            CheckUpdateButton.Click -= OnCheckUpdatesClick; // 移除检查更新事件
            CheckUpdateButton.Click -= Install; // 移除可能存在的Install事件
            CheckUpdateButton.Click += Install; // 添加下载事件
            CheckUpdateButton.IsEnabled = true;
        }

        private void Updateerror(string error)
        {
            StateText.Text = "更新出错";
            StateIcon.Visibility = Visibility.Visible;
            ProgressBar.Visibility = Visibility.Hidden;
            CheckUpdateButton.Content = "重试";
            CheckUpdateButton.Click -= Install; // 移除下载事件
            CheckUpdateButton.Click -= OnCheckUpdatesClick; // 移除可能存在的检查更新事件
            CheckUpdateButton.Click += OnCheckUpdatesClick; // 重新添加检查更新事件
            StateIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.ErrorCircle24;

            BrushConverter brushConverter = new BrushConverter();
            Brush? brush = (Brush?)brushConverter.ConvertFromString("#E53935");
            StateIcon.Foreground = brush;

            ErrorCard.Visibility = Visibility.Visible;
            errorText.Text = $"更新出错：{error}请检查Internet连接和对Github的连通性！";

            // 重置下载状态
            isDownloading = false;
            Properties.Settings.Default.IsDownloading = false;
            Properties.Settings.Default.Save();
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
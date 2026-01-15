using lanpingcj.Views.Pages;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows;
using Windows.Foundation.Collections;
using Wpf.Ui.Controls;

namespace lanpingcj
{
    public partial class MoreInfo : FluentWindow
    {

		private static Mutex? _appMutex; 
		private static bool _hasHandle = false;
		private string localFileName = "latest.exe";
		private bool isDownloading = false;
		private static bool IsAlreadyRunning()
		{
			string mutexName = $"Global\\{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}_MoreInfo_Mutex";

			try
			{
				_appMutex = new Mutex(true, mutexName, out _hasHandle);

				
				if (!_hasHandle)
				{
					return true; 
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"检查Mutex失败: {ex.Message}");
			
				_hasHandle = true;
			}

			return false; 
		}
		private static void ReleaseMutex()
		{
			if (_hasHandle && _appMutex != null)
			{
				_appMutex.ReleaseMutex();
				_appMutex.Dispose();
				_hasHandle = false;
			}
		}
		public async Task<(string Version, string Mandatory)> GetVersion()
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

            
            return (version, mandatory);
        }

        [Obsolete]
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
		private string FormatFileSize(long bytes)
		{
			string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
			int i = 0;
			double dblBytes = bytes;

			while (Math.Round(dblBytes / 1024) >= 1)
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

			try
			{
				// 显示开始Toast
				ShowSimpleToast("开始下载更新", "正在连接服务器...", false);

				using (HttpClient client = new HttpClient())
				{
					var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
					long? totalBytes = response.Content.Headers.ContentLength;

					using (var stream = await response.Content.ReadAsStreamAsync())
					using (var fileStream = File.Create(localFileName))
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
									UpdateSimpleToast(
										$"下载中... {percentage}%",
										$"{FormatFileSize(totalBytesRead)} / {FormatFileSize(totalBytes.Value)}"
									);
								}
							}
						}
					}
				}

				// 显示完成Toast
				ShowSimpleToast("下载完成", "点击安装更新", true);

				// 执行文件
				
			}
			catch (Exception ex)
			{
				ShowSimpleToast("下载失败", ex.Message, true);
			}
		}

		private void ShowSimpleToast(string title, string message, bool withSound)
		{
			var builder = new ToastContentBuilder()
				.AddText(title)
				.AddText(message);

		

			builder.Show();
		}

		private void UpdateSimpleToast(string title, string message)
		{
			
			try
			{
				var builder = new ToastContentBuilder()
					.AddText(title)
					.AddText(message)
					.AddAudio(new ToastAudio() { Silent = true });

				builder.Show(toast =>
				{
					toast.Tag = "downloadProgress";
					toast.Group = "updates";
				});
			}
			catch { }
		}
		
		
		private void ActivateExistingWindow()
		{
			try
			{
				var existingWindow = Application.Current.Windows.OfType<MoreInfo>().FirstOrDefault();

				if (existingWindow != null)
				{
					if (existingWindow.WindowState == WindowState.Minimized)
					{
						existingWindow.WindowState = WindowState.Normal;
					}

					existingWindow.Activate();
					existingWindow.Topmost = true;
					existingWindow.Topmost = false;
					existingWindow.Focus();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"激活现有窗口失败: {ex.Message}");
			}
		}

        [Obsolete]
        public MoreInfo()
        {
			//bool createdNew;
			this.Closed += (sender, e) =>
			{
				ReleaseMutex();
			};
			//mutex = new Mutex(true);
			if (IsAlreadyRunning())
			{

				ActivateExistingWindow();

				// 关闭当前尝试创建的窗口
				this.Close();
				return; // 直接返回，不继续初始化
			}
			InitializeComponent();
			ToastNotificationManagerCompat.OnActivated += toastArgs =>
			{
				// Obtain the arguments from the notification
				ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

				// Obtain any user input (text boxes, menu selections) from the notification
				ValueSet userInput = toastArgs.UserInput;

				// Need to dispatch to UI thread if performing UI operations
				Application.Current.Dispatcher.Invoke(delegate
				{
					// TODO: Show the corresponding content
					//MessageBox.Show("Toast activated. Args: " + toastArgs.Argument);
					//ToastNotificationManagerCompat_OnActivated(toastArgs);
					Process.Start(new ProcessStartInfo
					{
						FileName = localFileName,
						UseShellExecute = true
					});
				});
			};
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
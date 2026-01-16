using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Windows;
using System.Windows.Ink;
using Windows.Foundation.Collections;

namespace lanpingcj
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // System.Threading.Mutex mutex;

        public App()
        {
            //this.Startup += new StartupEventHandler(App_Startup);
            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;

                // Need to dispatch to UI thread if performing UI operations
                if (args.TryGetValue("action", out string action))
                {
                    switch (action)
                    {
                        case "Download":
                            Debug.WriteLine("这是一个DownloadToast!");
                            break;

                        case "RunApp":
                            // 处理忽略警告逻辑
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "latest.exe",
                                UseShellExecute = true  // 使用系统外壳执行
                            });
                            break;

                        case "OpenMoreInfo":
                            var appDispatcher = Application.Current?.Dispatcher;
                            if (appDispatcher != null)
                            {
                                appDispatcher.BeginInvoke(new Action(() =>
                                {
                                    var moreInfo = new MoreInfo();
                                    moreInfo.ShowDialog();
                                }));
                            }
                            else
                            {
                                var t = new Thread(() =>
                                {
                                    var moreInfo = new MoreInfo();
                                    moreInfo.ShowDialog();
                                    System.Windows.Threading.Dispatcher.Run();
                                });
                                t.SetApartmentState(ApartmentState.STA);
                                t.Start();
                            }
                            break;
                    }
                }
                ;
            };


        }
    }
}




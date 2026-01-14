using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows;
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
                Application.Current.Dispatcher.Invoke(delegate
                {
                    // TODO: Show the corresponding content
                    MoreInfo MoreInfo = new MoreInfo();
                    MoreInfo.ShowDialog();
                });
            };
        }

        
    }
}




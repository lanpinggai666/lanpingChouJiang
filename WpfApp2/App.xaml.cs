using System.Windows;

namespace lanpingcj
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;

        public App()
        {
            this.Startup += new StartupEventHandler(App_Startup);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {

            try
            {
                bool ret;
                mutex = new System.Threading.Mutex(true, "lanpingcj", out ret);
                if (!ret) Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动错误: {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}




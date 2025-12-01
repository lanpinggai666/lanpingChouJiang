using System.Windows;
using Wpf.Ui.Controls;
using WpfApp2.Views.Pages;

namespace WpfApp2
{
    public partial class Window2 : FluentWindow
    {
        public Window2()
        {
            InitializeComponent();

            // 订阅 Loaded 事件，确保 UI 完全加载后再进行导航
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 确保在 UI 完全加载后再进行导航
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
                }
            });
        }
    }
}
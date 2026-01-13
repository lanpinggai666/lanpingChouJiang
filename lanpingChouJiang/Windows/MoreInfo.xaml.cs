using lanpingcj.Views.Pages;
using System.Windows;
using System;
using System.Speech.Synthesis;
using Wpf.Ui.Controls;
using System.Diagnostics;

namespace lanpingcj
{
    public partial class MoreInfo : FluentWindow
    {
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
        }

        //
    }
}
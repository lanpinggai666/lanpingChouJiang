using System.Windows.Controls;
using System.Windows;

namespace WpfApp2.Views.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadCurrentSoundSetting();
        }

        // 加载当前声音设置
        private void LoadCurrentSoundSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool soundEnabled = AppSettings.SoundEnabled;
                SoundComboBox.SelectedIndex = soundEnabled ? 0 : 1;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                SoundComboBox.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine($"加载声音设置失败: {ex.Message}");
            }
        }

        // ComboBox选择改变事件
        private void SoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundComboBox.SelectedItem != null && IsLoaded)
            {
                try
                {
                    bool soundEnabled = SoundComboBox.SelectedIndex == 0;

                    // 保存到自定义设置
                    AppSettings.SoundEnabled = soundEnabled;
                    AppSettings.Save();

                    // 应用设置
                    ApplySoundSetting(soundEnabled);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存声音设置失败: {ex.Message}");
                }
            }
        }

        // 应用声音设置
        private void ApplySoundSetting(bool soundEnabled)
        {
            // 这里可以添加即时生效的逻辑
            System.Diagnostics.Debug.WriteLine($"声音设置已更新: {(soundEnabled ? "开启" : "关闭")}");
        }
    }
}
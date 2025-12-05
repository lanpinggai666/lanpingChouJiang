using System.Text;
using System.Windows.Controls;
using System.IO;
namespace lanpingcj.Views.Pages
{
    public partial class SettingsPage : Page
    {
        //bool SoundOpen = Properties.Settings.Default.SoundEnabled;
        public string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public SettingsPage()
        {
            InitializeComponent();
            LoadCurrentSoundSetting();
            LoadDuplicateSetting();
            LoadTTSSetting();
        }

        // 加载当前声音设置
        private void LoadCurrentSoundSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool Duplicatea = Properties.Settings.Default.Duplicate;
                Duplicate.SelectedIndex = Duplicatea ? 0 : 1;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                Duplicate.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine($"加载声音设置失败: {ex.Message}");
            }
        }
        private void LoadTTSSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool TTS_open = Properties.Settings.Default.tts;
                tts_open.SelectedIndex = TTS_open ? 0 : 1;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                Duplicate.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine($"加载声音设置失败: {ex.Message}");
            }
        }
        //加载点名不重复设置
        private void LoadDuplicateSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool soundEnabled = Properties.Settings.Default.SoundEnabled;
                SoundComboBox.SelectedIndex = soundEnabled ? 0 : 1;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                SoundComboBox.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
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
                    Properties.Settings.Default.SoundEnabled = soundEnabled;
                    Properties.Settings.Default.Save();

                    // 应用设置
                   // ApplySoundSetting(soundEnabled);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存声音设置失败: {ex.Message}");
                }
            }
        }
        private void ttsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tts_open.SelectedItem != null && IsLoaded)
            {
                try
                {
                    bool TTS_open = tts_open.SelectedIndex == 0;

                    // 保存到自定义设置
                    Properties.Settings.Default.tts = TTS_open;
                    Properties.Settings.Default.Save();

                    // 应用设置
                    // ApplySoundSetting(soundEnabled);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存tts设置失败: {ex.Message}");
                }
            }
        }
        private void DuplicateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Duplicate.SelectedItem != null && IsLoaded)
            {
                try
                {
                    bool DuplicateEnabled = Duplicate.SelectedIndex == 0;

                    // 保存到自定义设置
                    Properties.Settings.Default.Duplicate = DuplicateEnabled;
                    Properties.Settings.Default.Save();

                    // 应用设置
                    //ApplySoundSetting(DuplicateEnabled);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
                }
            }
        }
        // 应用声音设置
        void Restart(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
            // MessageBox.Show("已经重置点名不重复！",
            // "提示",
            // MessageBoxButton.OK,
            // MessageBoxImage.Information);
            Properties.Settings.Default.IsMain = false;
            Properties.Settings.Default.Save();
            Window3 w3 = new Window3();
            w3.NewTittle = "提示";
            w3.NewContent = "已经重置点名不重复！";
            w3.ShowDialog();
        }
    }
}
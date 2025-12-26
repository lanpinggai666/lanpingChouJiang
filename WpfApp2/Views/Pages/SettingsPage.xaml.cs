using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace lanpingcj.Views.Pages
{
   
    public partial class SettingsPage : Page
    {
        public string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public class BooleanToOnOffConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (bool)value ? "开启" : "关闭";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        public SettingsPage()
        {
            InitializeComponent();
            LoadCurrentSoundSetting();
            LoadDuplicateSetting();
            LoadTTSSetting();
            LoadgailvSetting();
        }

        // 加载当前声音设置
        private void LoadCurrentSoundSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool soundEnabled = Properties.Settings.Default.SoundEnabled;
                SoundToggleSwitch.IsChecked = soundEnabled;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                SoundToggleSwitch.IsChecked = false;
                System.Diagnostics.Debug.WriteLine($"加载声音设置失败: {ex.Message}");
            }
        }

        private void LoadTTSSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool ttsOpen = Properties.Settings.Default.tts;
                TTSToggleSwitch.IsChecked = ttsOpen;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                TTSToggleSwitch.IsChecked = false;
                System.Diagnostics.Debug.WriteLine($"加载TTS设置失败: {ex.Message}");
            }
        }

        //加载点名不重复设置
        private void LoadDuplicateSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool duplicateEnabled = Properties.Settings.Default.Duplicate;
                DuplicateToggleSwitch.IsChecked = duplicateEnabled;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                DuplicateToggleSwitch.IsChecked = false;
                System.Diagnostics.Debug.WriteLine($"加载点名不重复设置失败: {ex.Message}");
            }
        }

        private void LoadgailvSetting()
        {
            try
            {
                // 从自定义设置类加载
                bool gailvEnabled = Properties.Settings.Default.gailv1;
                ProbabilityToggleSwitch.IsChecked = gailvEnabled;
            }
            catch (System.Exception ex)
            {
                // 如果加载失败，使用默认值
                ProbabilityToggleSwitch.IsChecked = false;
                System.Diagnostics.Debug.WriteLine($"加载概率设置失败: {ex.Message}");
            }
        }

        // 声音开关状态改变事件
        private void SoundToggleSwitch_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveSoundSetting(true);
        }

        private void SoundToggleSwitch_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveSoundSetting(false);
        }

        private void SaveSoundSetting(bool isEnabled)
        {
            if (IsLoaded)
            {
                try
                {
                    // 保存到自定义设置
                    Properties.Settings.Default.SoundEnabled = isEnabled;
                    Properties.Settings.Default.Save();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存声音设置失败: {ex.Message}");
                }
            }
        }

        // 概率开关状态改变事件
        private void ProbabilityToggleSwitch_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveGailvSetting(true);
        }
        // 新增：重置概率平衡数据方法
        private void Reset_Probability(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");

            // 处理所有可能的名单文件
            string[] files = { "mindan.txt", "Boy_mindan.txt", "Girl_mindan.txt", "Shengwu_mindan.txt" };

            bool anyFileProcessed = false;

            foreach (string file in files)
            {
                string filePath = System.IO.Path.Combine(MindanPath, file);
                if (File.Exists(filePath))
                {
                    try
                    {
                        // 读取文件内容
                        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                        List<string> cleanedLines = new List<string>();

                        // 处理每一行，删除#和后面的数字
                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                // 查找#的位置
                                int hashIndex = line.IndexOf('#');
                                if (hashIndex >= 0)
                                {
                                    // 只保留#之前的部分（姓名）
                                    string cleanedName = line.Substring(0, hashIndex).Trim();
                                    if (!string.IsNullOrEmpty(cleanedName))
                                    {
                                        cleanedLines.Add(cleanedName);
                                    }
                                }
                                else
                                {
                                    // 如果没有#，保留原样
                                    cleanedLines.Add(line.Trim());
                                }
                            }
                        }

                        // 写回文件
                        if (cleanedLines.Count > 0)
                        {
                            File.WriteAllLines(filePath, cleanedLines, Encoding.UTF8);
                            anyFileProcessed = true;
                        }
                        else
                        {
                            // 如果文件为空，写入空文件
                            File.WriteAllText(filePath, "", Encoding.UTF8);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 错误处理
                        Window3 w3 = new Window3();
                        w3.NewTittle = "错误";
                        w3.NewContent = $"处理文件 {file} 时出错: {ex.Message}";
                        w3.ShowDialog();
                        return;
                    }
                }
            }

            // 显示结果
            Window3 resultWindow = new Window3();
            resultWindow.NewTittle = "提示";

            if (anyFileProcessed)
            {
                resultWindow.NewContent = "概率平衡数据已重置成功，所有计数已清零！";
            }
            else
            {
                resultWindow.NewContent = "未找到任何名单文件，无需重置。";
            }

            resultWindow.ShowDialog();

            
            
        }

        private void ProbabilityToggleSwitch_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveGailvSetting(false);
        }

        private void SaveGailvSetting(bool isEnabled)
        {
            if (IsLoaded)
            {
                try
                {
                    // 保存到自定义设置
                    Properties.Settings.Default.gailv1 = isEnabled;
                    Properties.Settings.Default.Save();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存概率设置失败: {ex.Message}");
                }
            }
        }

        // TTS开关状态改变事件
        private void TTSToggleSwitch_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveTTSSetting(true);
        }

        private void TTSToggleSwitch_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveTTSSetting(false);
        }

        private void SaveTTSSetting(bool isEnabled)
        {
            if (IsLoaded)
            {
                try
                {
                    // 保存到自定义设置
                    Properties.Settings.Default.tts = isEnabled;
                    Properties.Settings.Default.Save();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存TTS设置失败: {ex.Message}");
                }
            }
        }

        // 点名不重复开关状态改变事件
        private void DuplicateToggleSwitch_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveDuplicateSetting(true);
        }

        private void DuplicateToggleSwitch_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveDuplicateSetting(false);
        }

        private void SaveDuplicateSetting(bool isEnabled)
        {
            if (IsLoaded)
            {
                try
                {
                    // 保存到自定义设置
                    Properties.Settings.Default.Duplicate = isEnabled;
                    Properties.Settings.Default.Save();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存点名不重复设置失败: {ex.Message}");
                }
            }
        }

        // 重置点名不重复
        private void Restart(object sender, System.Windows.RoutedEventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");

            try
            {
                File.Delete(AlreadyPath);
                File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);

                Properties.Settings.Default.IsMain = false;
                Properties.Settings.Default.Save();

                Window3 w3 = new Window3();
                w3.NewTittle = "提示";
                w3.NewContent = "已经重置点名不重复！";
                w3.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重置点名不重复失败: {ex.Message}");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace lanpingcj
{
    public partial class ChoseMoreMan : FluentWindow
    {
        private string _fullTxtPath = string.Empty;

        public ChoseMoreMan()
        {
            InitializeComponent();
            InitializeMindanPath();

            int lineCount = 0;
            if (!string.IsNullOrEmpty(_fullTxtPath) && File.Exists(_fullTxtPath))
            {
                var lines = File.ReadAllLines(_fullTxtPath, Encoding.UTF8)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToList();
                lineCount = lines.Count;
            }

            NumberComboBox.ItemsSource = Enumerable.Range(1, lineCount).ToList();
        }

        private void InitializeMindanPath()
        {
            try
            {
                string configPath = Properties.Settings.Default.CurrentConfigFile;
                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath)) return;

                string jsonString = File.ReadAllText(configPath, Encoding.UTF8);
                using var doc = JsonDocument.Parse(jsonString);

                if (doc.RootElement.TryGetProperty("mindan_path", out JsonElement pathElement))
                {
                    string? txtPathValue = pathElement.GetString();
                    if (string.IsNullOrEmpty(txtPathValue)) return;

                    if (Path.IsPathRooted(txtPathValue))
                    {
                        _fullTxtPath = txtPathValue;
                    }
                    else
                    {
                        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lanpingcj_mindan");
                        _fullTxtPath = Path.Combine(folderPath, txtPathValue);
                    }
                }
            }
            catch
            {
                _fullTxtPath = string.Empty;
            }
        }

        void OK(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_fullTxtPath))
            {
                System.Windows.MessageBox.Show("未找到有效的名单配置文件，请先在设置中配置！", "错误", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string mindanDir = Path.GetDirectoryName(_fullTxtPath) ?? string.Empty;
            string alreadyPath = Path.Combine(mindanDir, "Already.txt");

            if (!File.Exists(_fullTxtPath))
            {
                System.Windows.MessageBox.Show("名单文件不存在！", "错误", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(alreadyPath))
            {
                File.WriteAllText(alreadyPath, "", Encoding.UTF8);
            }

            List<string> allLines = File.ReadAllLines(_fullTxtPath, Encoding.UTF8)
                               .Select(s => s ?? string.Empty)
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrWhiteSpace(s))
                               .Select(s =>
                               {
                                   int hashIndex = s.IndexOf('#');
                                   return hashIndex >= 0 ? s.Substring(0, hashIndex).Trim() : s;
                               })
                               .ToList();

            if (allLines.Count == 0)
            {
                System.Windows.MessageBox.Show("名单文件是空的！", "提示", System.Windows.MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NumberComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("请选择抽取数量。", "提示", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int selectedValue = (int)NumberComboBox.SelectedItem;

            HashSet<string> alreadyLines = File.ReadAllLines(alreadyPath, Encoding.UTF8)
                                   .Select(s => s ?? string.Empty)
                                   .Select(s => s.Trim())
                                   .Where(s => !string.IsNullOrWhiteSpace(s))
                                   .ToHashSet(StringComparer.Ordinal);

            List<string> available = allLines.Where(n => !alreadyLines.Contains(n)).ToList();


            if (available.Count < selectedValue)
            {
                File.WriteAllText(alreadyPath, "", Encoding.UTF8);
                available = new List<string>(allLines);

            }

            Random rng = new Random();
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                string tmp = available[i];
                available[i] = available[j];
                available[j] = tmp;
            }

            List<string> picked = available.Take(selectedValue).ToList();

            // 记录已抽中的人
            File.AppendAllLines(alreadyPath, picked, Encoding.UTF8);

            string joined = string.Join(", ", picked);

            Properties.Settings.Default.IsMain = false;
            Properties.Settings.Default.Save();

            MessageBox MB = new MessageBox();
            MB.NewTittle = "抽奖结果";
            MB.NewContent = $"幸运儿是：{joined}";
            MB.New_extra_text = string.Empty;

            MB.ShowDialog();
            this.Close();
        }

        void Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
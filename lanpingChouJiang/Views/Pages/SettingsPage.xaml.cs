using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.Text.Json;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lanpingcj.Views.Pages
{
    public partial class SettingsPage : Page
    {
        private bool _isUpdatingUI = false;

        public class ConfigFileItem
        {
            public string FilePath { get; set; } = string.Empty;
            public string ConfigName { get; set; } = string.Empty;
            public bool IsActionItem { get; set; }
            public string ActionType { get; set; } = string.Empty;
            public Wpf.Ui.Controls.SymbolRegular Icon { get; set; } = Wpf.Ui.Controls.SymbolRegular.Document20;

            public string DisplayText
            {
                get
                {
                    if (IsActionItem) return ConfigName;
                    return string.IsNullOrEmpty(ConfigName) ? (Path.GetFileName(FilePath) ?? string.Empty) : ConfigName;
                }
            }

            public override string ToString() => DisplayText;
        }

        public SettingsPage()
        {
            InitializeComponent();

            ValidateAndApplyConfig(ConfigService.CurrentConfigPath);

            version.Text = Properties.Settings.Default.ThisVersion ?? "1.0.0";
            version2.Text = $"v{Properties.Settings.Default.ThisVersion ?? "1.0.0"}";
        }

        private ContentDialogHost? GetDialogHost()
        {
            var window = Window.GetWindow(this);
            return window?.FindName("RootContentDialogPresenter") as ContentDialogHost;
        }

        private bool IsValidProgramJson(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath, Encoding.UTF8);
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    return doc.RootElement.TryGetProperty("ConfigName", out _) ||
                           doc.RootElement.TryGetProperty("mindan_path", out _) ||
                           doc.RootElement.TryGetProperty("Tittle", out _);
                }
            }
            catch
            {
                return false;
            }
        }

        private string GenerateRandomFileName(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()) + ".txt";
        }

        private async Task PromptRestart()
        {
            var host = GetDialogHost();
            if (host == null) return;

            var dialog = new ContentDialog(host)
            {
                Title = "配置文件已更新",
                Content = "请重启应用程序以保存更改！                                       ",
                PrimaryButtonText = "立即重启",
                CloseButtonText = "稍后",
                PrimaryButtonAppearance = ControlAppearance.Primary
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    System.Diagnostics.Process.Start(exePath);
                    Application.Current.Shutdown();
                }
            }
        }

        private void LoadConfigHistory()
        {
            if (Properties.Settings.Default.ConfigHistory == null)
            {
                Properties.Settings.Default.ConfigHistory = new StringCollection();
            }

            var originalList = Properties.Settings.Default.ConfigHistory.Cast<string>().ToList();
            var cleanedList = originalList
                .Where(path =>
                    !string.IsNullOrWhiteSpace(path) &&
                    File.Exists(path) &&
                    Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".deps.json") &&
                    !path.EndsWith(".runtimeconfig.json")
                )
                .Distinct()
                .ToList();

            Properties.Settings.Default.ConfigHistory.Clear();
            foreach (var path in cleanedList)
            {
                Properties.Settings.Default.ConfigHistory.Add(path);
            }

            string currentConfig = ConfigService.CurrentConfigPath;

            if (File.Exists(currentConfig) && !Properties.Settings.Default.ConfigHistory.Contains(currentConfig))
            {
                Properties.Settings.Default.ConfigHistory.Add(currentConfig);
                cleanedList.Add(currentConfig);
            }

            Properties.Settings.Default.Save();

            var displayItems = new List<ConfigFileItem>();
            ConfigFileItem? currentItem = null;

            foreach (var path in cleanedList)
            {
                string name = string.Empty;
                try
                {
                    string jsonString = File.ReadAllText(path, Encoding.UTF8);
                    Config? config = JsonSerializer.Deserialize<Config>(jsonString, ConfigService.JsonOptions);
                    if (config != null) name = config.ConfigName ?? string.Empty;
                }
                catch { }

                var item = new ConfigFileItem { FilePath = path, ConfigName = name };
                displayItems.Add(item);
                if (path == currentConfig) currentItem = item;
            }

            displayItems.Add(new ConfigFileItem
            {
                ConfigName = "新建配置文件...",
                IsActionItem = true,
                ActionType = "New",
                Icon = Wpf.Ui.Controls.SymbolRegular.Add12
            });

            displayItems.Add(new ConfigFileItem
            {
                ConfigName = "浏览本地文件...",
                IsActionItem = true,
                ActionType = "Browse",
                Icon = Wpf.Ui.Controls.SymbolRegular.Folder24
            });

            if (cleanedList.Count > 1)
            {
                displayItems.Add(new ConfigFileItem
                {
                    ConfigName = "移除当前配置...",
                    IsActionItem = true,
                    ActionType = "Remove",
                    Icon = Wpf.Ui.Controls.SymbolRegular.Delete24
                });
            }

            _isUpdatingUI = true;
            ConfigFileComboBox.ItemsSource = displayItems;
            ConfigFileComboBox.SelectedItem = currentItem;
            _isUpdatingUI = false;
        }

        private async void ConfigFileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI) return;

            if (ConfigFileComboBox.SelectedItem is ConfigFileItem selectedItem)
            {
                if (selectedItem.IsActionItem)
                {
                    await Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        _isUpdatingUI = true;
                        ConfigFileComboBox.SelectedItem = ConfigFileComboBox.Items.OfType<ConfigFileItem>()
                            .FirstOrDefault(i => !i.IsActionItem && i.FilePath == Properties.Settings.Default.CurrentConfigFile);

                        _isUpdatingUI = false;

                        if (selectedItem.ActionType == "New") await CreateNewConfig();
                        else if (selectedItem.ActionType == "Browse") await BrowseConfig();
                        else if (selectedItem.ActionType == "Remove") await RemoveCurrentConfig();
                    }));
                    return;
                }

                if (selectedItem.FilePath != Properties.Settings.Default.CurrentConfigFile)
                {
                    ValidateAndApplyConfig(selectedItem.FilePath);
                    await PromptRestart();
                }
            }
        }

        private async Task CreateNewConfig()
        {
            string configNameInput = await PromptForConfigName("输入新建的配置文件名");
            if (string.IsNullOrWhiteSpace(configNameInput)) return;

            try
            {
                string configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lanpingcj_configs");
                if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);

                string safeName = string.Join("_", configNameInput.Split(Path.GetInvalidFileNameChars()));
                string newFilePath = Path.Combine(configFolder, $"{safeName}_{DateTime.Now.Ticks}.json");

                string mindanFolder = ConfigService.MindanFolder;
                if (!Directory.Exists(mindanFolder)) Directory.CreateDirectory(mindanFolder);

                string randomTxtName = GenerateRandomFileName(12);
                string mindanFullPath = Path.Combine(mindanFolder, randomTxtName);
                File.WriteAllText(mindanFullPath, "示例姓名#0", Encoding.UTF8);

                var newConfig = new Config
                {
                    ConfigName = configNameInput.Trim(),
                    mindan_path = randomTxtName
                };

                File.WriteAllText(newFilePath, JsonSerializer.Serialize(newConfig, ConfigService.JsonOptions), Encoding.UTF8);
                ValidateAndApplyConfig(newFilePath);
                await PromptRestart();
            }
            catch (Exception ex)
            {
                new WarningMeassageBox { errorNewContent = $"新建失败：\n{ex.Message}" }.ShowDialog();
            }
        }

        private async Task BrowseConfig()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "配置文件 (*.json)|*.json",
                Title = "选择配置文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedPath = openFileDialog.FileName;
                if (selectedPath.EndsWith(".deps.json") || selectedPath.EndsWith(".runtimeconfig.json")) return;

                if (!IsValidProgramJson(selectedPath))
                {
                    new WarningMeassageBox { errorNewContent = "这不是一个有效的抽奖配置文件！" }.ShowDialog();
                    return;
                }

                string configNameInput = await PromptForConfigName("为导入的配置文件命名");
                if (string.IsNullOrWhiteSpace(configNameInput)) return;

                try
                {
                    string jsonString = File.ReadAllText(selectedPath, Encoding.UTF8);
                    Config config = JsonSerializer.Deserialize<Config>(jsonString, ConfigService.JsonOptions) ?? new Config();

                    config.ConfigName = configNameInput.Trim();
                    File.WriteAllText(selectedPath, JsonSerializer.Serialize(config, ConfigService.JsonOptions), Encoding.UTF8);
                }
                catch { }

                ValidateAndApplyConfig(selectedPath);
                await PromptRestart();
            }
        }

        private async Task RemoveCurrentConfig()
        {
            var host = GetDialogHost();
            if (host == null) return;

            var dialog = new ContentDialog(host)
            {
                Title = "移除配置文件",
                Content = "确定要从列表中移除当前的配置文件吗？（文件本体不会被删除）",
                PrimaryButtonText = "移除",
                CloseButtonText = "取消"
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                string current = Properties.Settings.Default.CurrentConfigFile ?? string.Empty;
                var history = Properties.Settings.Default.ConfigHistory;

                if (history != null && history.Contains(current))
                {
                    history.Remove(current);
                }

                string fallback = ConfigService.DefaultConfigPath;
                if (history != null && history.Count > 0)
                {
                    fallback = history[0] ?? fallback;
                }

                Properties.Settings.Default.CurrentConfigFile = fallback;
                Properties.Settings.Default.Save();

                ValidateAndApplyConfig(fallback);
                await PromptRestart();
            }
        }

        private async Task<string> PromptForConfigName(string placeholder)
        {
            string configNameInput = string.Empty;
            bool nameProvided = false;
            string currentPlaceholder = placeholder;

            var host = GetDialogHost();
            if (host == null) return string.Empty;

            while (!nameProvided)
            {
                var textBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = currentPlaceholder, Text = configNameInput };
                var dialog = new ContentDialog(host)
                {
                    Title = "命名配置文件",
                    Content = textBox,
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消"
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    configNameInput = textBox.Text;
                    if (!string.IsNullOrWhiteSpace(configNameInput))
                    {
                        nameProvided = true;
                    }
                    else
                    {
                        currentPlaceholder = "名称不能为空，请重新输入！";
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            return configNameInput;
        }

        private void ValidateAndApplyConfig(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath, Encoding.UTF8);
                Config? config = JsonSerializer.Deserialize<Config>(jsonString, ConfigService.JsonOptions);

                if (config == null) throw new Exception("文件格式不正确。");

                Properties.Settings.Default.CurrentConfigFile = filePath;

                if (Properties.Settings.Default.ConfigHistory == null)
                    Properties.Settings.Default.ConfigHistory = new StringCollection();

                if (!Properties.Settings.Default.ConfigHistory.Contains(filePath))
                    Properties.Settings.Default.ConfigHistory.Add(filePath);

                Properties.Settings.Default.Save();

                _isUpdatingUI = true;
                SoundToggleSwitch.IsChecked = config.Sound;
                TTSToggleSwitch.IsChecked = config.TTS;
                DuplicateToggleSwitch.IsChecked = !config.Repeat;
                ProbabilityToggleSwitch.IsChecked = config.Probability_balance;
                StudentIdToggleSwitch.IsChecked = config.Use_StudentsID;
                MinIdBox.Value = config.Min_StudentsID;
                MaxIdBox.Value = config.Max_StudentsID;
                LockToggleSwitch.IsChecked = config.Lock;
                LockPasswordBox.Password = config.Lock_Password ?? string.Empty;
                _isUpdatingUI = false;

                LoadConfigHistory();
            }
            catch (Exception ex)
            {
                WarningMeassageBox warningBox = new WarningMeassageBox { errorNewContent = $"加载失败！\n{ex.Message}" };
                warningBox.ShowDialog();
                LoadConfigHistory();
            }
        }

        // 写入配置文件后同步刷新主窗口的内存配置，改动立即生效、无需重启
        private void UpdateJsonConfig(Action<Config> updateAction)
        {
            ConfigService.UpdateCurrent(updateAction);
            Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.RefreshConfig();
        }

        private void SoundToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Sound = true);
        }

        private void SoundToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Sound = false);
        }

        private void TTSToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.TTS = true);
        }

        private void TTSToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.TTS = false);
        }

        private void DuplicateToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Repeat = false);
        }

        private void DuplicateToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Repeat = true);
        }

        private void ProbabilityToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Probability_balance = true);
        }

        private void ProbabilityToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Probability_balance = false);
        }

        private void StudentIdToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Use_StudentsID = true);
        }

        private void StudentIdToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Use_StudentsID = false);
        }

        private void IdRangeBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUI || MinIdBox == null || MaxIdBox == null) return;

            int min = (int)(MinIdBox.Value ?? 1);
            int max = (int)(MaxIdBox.Value ?? 40);
            if (min < 1) min = 1;
            if (max < min) max = min;

            UpdateJsonConfig(c =>
            {
                c.Min_StudentsID = min;
                c.Max_StudentsID = max;
            });
        }

        private async void LockToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUI) return;

            string password = LockPasswordBox.Password;
            if (string.IsNullOrEmpty(password))
            {
                _isUpdatingUI = true;
                LockToggleSwitch.IsChecked = false;
                _isUpdatingUI = false;

                var host = GetDialogHost();
                if (host != null)
                {
                    var dialog = new ContentDialog(host)
                    {
                        Title = "无法开启锁定",
                        Content = "请先在下方输入并保存密码，再开启设置锁定。",
                        CloseButtonText = "确定"
                    };
                    await dialog.ShowAsync();
                }
                return;
            }

            UpdateJsonConfig(c =>
            {
                c.Lock = true;
                c.Lock_Password = password;
            });
        }

        private void LockToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isUpdatingUI) UpdateJsonConfig(c => c.Lock = false);
        }

        private async void SaveLockPassword_Click(object sender, RoutedEventArgs e)
        {
            string password = LockPasswordBox.Password;
            bool lockEnabled = LockToggleSwitch.IsChecked == true;

            if (lockEnabled && string.IsNullOrEmpty(password))
            {
                var host = GetDialogHost();
                if (host != null)
                {
                    var dialog = new ContentDialog(host)
                    {
                        Title = "密码不能为空",
                        Content = "锁定已开启时不能将密码设为空，请先关闭锁定。",
                        CloseButtonText = "确定"
                    };
                    await dialog.ShowAsync();
                }
                return;
            }

            UpdateJsonConfig(c => c.Lock_Password = password);

            var host2 = GetDialogHost();
            if (host2 != null)
            {
                var dialog = new ContentDialog(host2)
                {
                    Title = "已保存",
                    Content = "设置锁密码已更新。",
                    CloseButtonText = "确定"
                };
                await dialog.ShowAsync();
            }
        }

        // 把当前名单里所有 "姓名#次数" 的计数清零
        private async void Reset_Probability(object sender, EventArgs e)
        {
            var config = ConfigService.LoadCurrent();
            string? mindanPath = ConfigService.GetMindanFullPath(config);

            if (string.IsNullOrEmpty(mindanPath) || !File.Exists(mindanPath))
            {
                new WarningMeassageBox { errorNewContent = "未找到当前名单文件，无法重置概率！" }.ShowDialog();
                return;
            }

            try
            {
                var cleanedLines = File.ReadAllLines(mindanPath, Encoding.UTF8)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line =>
                    {
                        int hashIndex = line.IndexOf('#');
                        string name = hashIndex >= 0 ? line.Substring(0, hashIndex).Trim() : line.Trim();
                        return $"{name}#0";
                    })
                    .Where(line => line != "#0")
                    .ToList();

                File.WriteAllLines(mindanPath, cleanedLines, Encoding.UTF8);

                Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.ReloadRoster();

                var host = GetDialogHost();
                if (host != null)
                {
                    var dialog = new ContentDialog(host)
                    {
                        Title = "提示",
                        Content = "概率平衡已重置，立即生效。",
                        CloseButtonText = "确定"
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                new WarningMeassageBox { errorNewContent = $"重置概率失败：\n{ex.Message}" }.ShowDialog();
            }
        }

        // 清空主窗口的"点名不重复"已抽名单
        private async void Restart(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow?.ResetData();

            var host = GetDialogHost();
            if (host != null)
            {
                var dialog = new ContentDialog(host)
                {
                    Title = "提示",
                    Content = mainWindow != null ? "已经重置点名不重复！" : "未找到主窗口，请重启程序。",
                    CloseButtonText = "确定"
                };
                await dialog.ShowAsync();
            }
        }
    }
}

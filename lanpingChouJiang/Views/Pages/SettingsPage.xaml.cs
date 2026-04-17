using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Controls;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.Text.Json;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace lanpingcj.Views.Pages
{
    public partial class SettingsPage : Page
    {
        public string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lanpingcj_mindan").Replace("\\", "\\\\");
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

        public async Task<(string Version, string Mandatory)> GetVersion()
        {
            string url = "https://update.choujiang.lanpinggai.top/version";

            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            string content = await client.GetStringAsync(url);

            using StringReader reader = new StringReader(content);
            string version = (await reader.ReadLineAsync())?.Trim() ?? string.Empty;
            string mandatory = (await reader.ReadLineAsync())?.Trim() ?? string.Empty;

            return (version, mandatory);
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
        public async Task CheckUpdate()
        {
            var result = await GetVersion();

            if (!bool.TryParse(result.Mandatory, out bool mandatory)) mandatory = false;

            Version LatestVersion = new Version(result.Version);
            Version ThisVersion = new Version(Properties.Settings.Default.ThisVersion ?? "1.0.0");

            if (LatestVersion > ThisVersion)
            {
                var host = GetDialogHost();
                if (host == null) return;

                var Dialog = new ContentDialog(host);

                Dialog.Title = "有新版本可用!";
                Dialog.Content = $"当前版本：{ThisVersion}\n最新版本：{LatestVersion}\n";
                Dialog.PrimaryButtonText = "确定";
                Dialog.CloseButtonText = "关闭";
                Dialog.PrimaryButtonAppearance = ControlAppearance.Primary;
                Dialog.SecondaryButtonAppearance = ControlAppearance.Secondary;

                var Dialogresult = await Dialog.ShowAsync();
                if (Dialogresult == ContentDialogResult.Primary)
                {
                    await DownloadUpdate();
                }
            }
        }

        public async Task DownloadUpdate()
        {
            string downloadUrl = "https://update.choujiang.lanpinggai.top/latest.exe";
            string localFileName = "latest.exe";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(localFileName, fileBytes);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = localFileName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"错误: {ex.Message}");
            }
        }

        public SettingsPage()
        {
            InitializeComponent();

            string currentConfig = Properties.Settings.Default.CurrentConfigFile ?? string.Empty;
            if (string.IsNullOrEmpty(currentConfig) || !File.Exists(currentConfig))
            {
                currentConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Settings.Default.defaultConfig ?? "config.json");
            }

            ValidateAndApplyConfig(currentConfig);

            version.Text = Properties.Settings.Default.ThisVersion ?? "1.0.0";
            version2.Text = $"v{Properties.Settings.Default.ThisVersion ?? "1.0.0"}";
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

            string currentConfig = Properties.Settings.Default.CurrentConfigFile ?? string.Empty;
            if (string.IsNullOrEmpty(currentConfig) || !File.Exists(currentConfig))
            {
                currentConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Settings.Default.defaultConfig ?? "config.json");
            }

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
                    Config? config = JsonSerializer.Deserialize<Config>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
            string configNameInput = await PromptForConfigName("输入新建的配置文件名                                         ");
            if (string.IsNullOrWhiteSpace(configNameInput)) return;

            try
            {
                string configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lanpingcj_configs");
                if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);

                string safeName = string.Join("_", configNameInput.Split(Path.GetInvalidFileNameChars()));
                string newFilePath = Path.Combine(configFolder, $"{safeName}_{DateTime.Now.Ticks}.json");

                string actualFolderPath = folderPath.Replace("\\\\", "\\");
                if (!Directory.Exists(actualFolderPath)) Directory.CreateDirectory(actualFolderPath);

                string randomTxtName = GenerateRandomFileName(12);
                string mindanFullPath = Path.Combine(actualFolderPath, randomTxtName);
                File.WriteAllText(mindanFullPath, "示例姓名#0", Encoding.UTF8);

                string template = $@"{{
    ""ConfigName"":""{configNameInput.Replace("\"", "\\\"")}"",
    ""mindan_path"":""{randomTxtName}"",
    ""Repeat"":true,
    ""Sound"":true,
    ""TTS"":true,
    ""Probability_balance"":true,
    ""Lock"":false,
    ""Lock_Password"":"""",
    ""Use_StudentsID"":false,
    ""Min_StudentsID"":1,
    ""Max_StudentsID"":40,
    ""Tittle"":""幸运儿""
}}";

                File.WriteAllText(newFilePath, template, Encoding.UTF8);
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
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
                    Config config = JsonSerializer.Deserialize<Config>(jsonString, options) ?? new Config();

                    config.ConfigName = configNameInput.Trim();
                    File.WriteAllText(selectedPath, JsonSerializer.Serialize(config, options), Encoding.UTF8);
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
                Content = "确定要从列表中移除当前的配置文件吗？                             ",
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

                string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Settings.Default.defaultConfig ?? "config.json");
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
                    Title = "命名配置文件                           ",
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
                Config? config = JsonSerializer.Deserialize<Config>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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

        private void UpdateJsonConfig(Action<Config> updateAction)
        {
            try
            {
                string currentConfig = Properties.Settings.Default.CurrentConfigFile ?? string.Empty;
                if (string.IsNullOrEmpty(currentConfig) || !File.Exists(currentConfig)) return;

                string jsonString = File.ReadAllText(currentConfig, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
                Config? config = JsonSerializer.Deserialize<Config>(jsonString, options);

                if (config != null)
                {
                    updateAction(config);
                    File.WriteAllText(currentConfig, JsonSerializer.Serialize(config, options), Encoding.UTF8);
                }
            }
            catch { }
        }

        public static void WriteManifest(bool Gettop)
        {
            var module = Process.GetCurrentProcess().MainModule;
            if (module == null) return;

            string exePath = module.FileName;
            string manifestPath = exePath + ".manifest";
            string uiAccessValue = Gettop ? "true" : "false";

            string xmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<assembly manifestVersion=""1.0"" xmlns=""urn:schemas-microsoft-com:asm.v1"">
  <assemblyIdentity version=""1.0.0.0"" name=""MyApplication.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""{uiAccessValue}"" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>";

            try
            {
                File.WriteAllText(manifestPath, xmlContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void TopmostToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            SaveTopSetting(true);
            WriteManifest(true);
        }

        private void TopmostToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveTopSetting(false);
            WriteManifest(false);
        }

        private async void SaveTopSetting(bool isEnabled)
        {
            if (IsLoaded)
            {
                try
                {
                    Properties.Settings.Default.Top = isEnabled;
                    Properties.Settings.Default.Save();
                    var moreInfoWindow = Application.Current.Windows.OfType<MoreInfo>().FirstOrDefault();
                    if (moreInfoWindow != null)
                    {
                        await moreInfoWindow.TopDialog();
                    }
                    else
                    {
                        MoreInfo more = new MoreInfo();
                        await more.TopDialog();
                    }
                }
                catch { }
            }
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

        private void Reset_Probability(object sender, EventArgs e)
        {
            string MindanPath = Path.Combine(documentsPath, "mindan");
            string[] files = { "mindan.txt", "Boy_mindan.txt", "Girl_mindan.txt", "Shengwu_mindan.txt" };

            foreach (string file in files)
            {
                string filePath = Path.Combine(MindanPath, file);
                if (File.Exists(filePath))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                        List<string> cleanedLines = new List<string>();

                        foreach (string line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                int hashIndex = line.IndexOf('#');
                                if (hashIndex >= 0)
                                {
                                    string cleanedName = line.Substring(0, hashIndex).Trim();
                                    if (!string.IsNullOrEmpty(cleanedName))
                                    {
                                        cleanedLines.Add(cleanedName);
                                    }
                                }
                                else
                                {
                                    cleanedLines.Add(line.Trim());
                                }
                            }
                        }

                        if (cleanedLines.Count > 0)
                        {
                            File.WriteAllLines(filePath, cleanedLines, Encoding.UTF8);
                        }
                        else
                        {
                            File.WriteAllText(filePath, "", Encoding.UTF8);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox w3 = new MessageBox { NewTittle = "错误", NewContent = $"处理文件 {file} 时出错: {ex.Message}" };
                        w3.ShowDialog();
                        return;
                    }
                }
            }

            MessageBox resultWindow = new MessageBox { NewTittle = "提示", NewContent = "概率平衡已重置" };
            resultWindow.ShowDialog();
        }

        private void Restart(object sender, RoutedEventArgs e)
        {
            string MindanPath = Path.Combine(documentsPath, "mindan");
            string AlreadyPath = Path.Combine(MindanPath, "Already.txt");

            try
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.ResetData();

                MessageBox w3 = new MessageBox { NewTittle = "提示", NewContent = "已经重置点名不重复！" };
                w3.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    public class Config
    {
        public string? ConfigName { get; set; } = string.Empty;
        public string? mindan_path { get; set; } = string.Empty;
        public bool Repeat { get; set; } = true;
        public bool Sound { get; set; } = true;
        public bool TTS { get; set; } = true;
        public bool Probability_balance { get; set; } = true;
        public bool Lock { get; set; } = false;
        public string? Lock_Password { get; set; } = string.Empty;
        public bool Use_StudentsID { get; set; } = false;
        public int Min_StudentsID { get; set; } = 1;
        public int Max_StudentsID { get; set; } = 40;
        public string? Tittle { get; set; } = "幸运儿";
    }
}
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace lanpingcj.Views.Pages
{
    public partial class StudentManagePage : Page
    {
        // 数据模型：完美适配 名字#次数 格式
        public class StudentItem
        {
            public string RawData { get; set; } = string.Empty; // 原始数据，如 "张三#2"

            // 供 UI 显示的名字（提取 # 前面的部分）
            public string DisplayName
            {
                get
                {
                    if (string.IsNullOrEmpty(RawData)) return string.Empty;
                    int hashIndex = RawData.IndexOf('#');
                    return hashIndex >= 0 ? RawData.Substring(0, hashIndex) : RawData;
                }
            }

            // 提取被抽中的次数（# 后面的数字），保留此属性方便未来扩展UI
            public int DrawCount
            {
                get
                {
                    if (string.IsNullOrEmpty(RawData)) return 0;
                    int hashIndex = RawData.IndexOf('#');
                    if (hashIndex >= 0 && hashIndex < RawData.Length - 1)
                    {
                        if (int.TryParse(RawData.Substring(hashIndex + 1), out int count))
                        {
                            return count;
                        }
                    }
                    return 0;
                }
            }
        }

        private ObservableCollection<StudentItem> _students = new ObservableCollection<StudentItem>();
        private string _fullTxtPath = string.Empty;

        public StudentManagePage()
        {
            InitializeComponent();
            StudentListView.ItemsSource = _students;
            this.Loaded += (s, e) => LoadCurrentMindan();
        }

        // 获取 Fluent UI 弹窗宿主
        private ContentDialogHost? GetDialogHost()
        {
            var window = Window.GetWindow(this);
            return window?.FindName("RootContentDialogPresenter") as ContentDialogHost;
        }

        // 通用消息提示弹窗
        private async void ShowMessageDialog(string title, string content)
        {
            var host = GetDialogHost();
            if (host == null) return;

            var dialog = new ContentDialog(host)
            {
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }

        // 重启提示弹窗
        private async Task PromptRestart()
        {
            var host = GetDialogHost();
            if (host == null) return;

            var dialog = new ContentDialog(host)
            {
                Title = "修改已保存",
                Content = "名单已更新！\n需要重启应用程序才能使新名单完全生效。\n是否立即重启？",
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

        // 加载当前名单
        private void LoadCurrentMindan()
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

                    if (File.Exists(_fullTxtPath))
                    {
                        var lines = File.ReadAllLines(_fullTxtPath, Encoding.UTF8)
                                        .Where(line => !string.IsNullOrWhiteSpace(line))
                                        .ToList();

                        _students.Clear();
                        foreach (var line in lines)
                        {
                            _students.Add(new StudentItem { RawData = line });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog("加载失败", $"读取出错: {ex.Message}");
            }
        }

        // 保存到文件
        private bool SaveToFile()
        {
            if (string.IsNullOrEmpty(_fullTxtPath))
            {
                ShowMessageDialog("提示", "未找到有效的名单路径！");
                return false;
            }

            try
            {
                // 保存时，写入的是包含 # 和次数的完整 RawData
                var linesToSave = _students.Select(s => s.RawData).ToList();
                File.WriteAllLines(_fullTxtPath, linesToSave, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                ShowMessageDialog("保存失败", ex.Message);
                return false;
            }
        }

        // 按钮：添加
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(input)) return;

            var newNames = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            bool added = false;

            foreach (var name in newNames)
            {
                string cleanName = name.Trim();
                if (!string.IsNullOrEmpty(cleanName))
                {
                    // 适配概率平衡格式：如果没有带有 #，默认补齐 #0
                    if (!cleanName.Contains("#"))
                    {
                        cleanName += "#0";
                    }
                    _students.Add(new StudentItem { RawData = cleanName });
                    added = true;
                }
            }

            if (added)
            {
                InputTextBox.Clear();
                if (SaveToFile()) await PromptRestart();
            }
        }

        // 按钮：删除
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (StudentListView.SelectedItems == null || StudentListView.SelectedItems.Count == 0) return;

            var selected = StudentListView.SelectedItems.Cast<StudentItem>().ToList();
            foreach (var item in selected)
            {
                _students.Remove(item);
            }

            if (SaveToFile()) await PromptRestart();
        }

        // 按钮：清空
        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var host = GetDialogHost();
            if (host == null) return;

            var dialog = new ContentDialog(host)
            {
                Title = "警告                                                     ",
                Content = "确定要清空名单吗？",
                PrimaryButtonText = "确定清空",
                CloseButtonText = "取消",
                PrimaryButtonAppearance = ControlAppearance.Danger
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _students.Clear();
                if (SaveToFile()) await PromptRestart();
            }
        }
    }
}
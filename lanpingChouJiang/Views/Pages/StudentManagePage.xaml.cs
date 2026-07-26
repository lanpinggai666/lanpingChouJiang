using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace lanpingcj.Views.Pages
{
    public partial class StudentManagePage : Page
    {
        public class StudentItem
        {
            public string RawData { get; set; } = string.Empty;
            public string DisplayName
            {
                get
                {
                    if (string.IsNullOrEmpty(RawData)) return string.Empty;
                    int hashIndex = RawData.IndexOf('#');
                    return hashIndex >= 0 ? RawData.Substring(0, hashIndex) : RawData;
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

        private ContentDialogHost? GetDialogHost()
        {
            var window = Window.GetWindow(this);
            return window?.FindName("RootContentDialogPresenter") as ContentDialogHost;
        }

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

        private void LoadCurrentMindan()
        {
            try
            {
                var config = ConfigService.LoadCurrent();
                _fullTxtPath = ConfigService.GetMindanFullPath(config) ?? string.Empty;

                if (string.IsNullOrEmpty(_fullTxtPath) || !File.Exists(_fullTxtPath)) return;

                var lines = File.ReadAllLines(_fullTxtPath, Encoding.UTF8)
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .ToList();

                _students.Clear();
                foreach (var name in lines)
                {
                    _students.Add(new StudentItem { RawData = name });
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog("加载失败", $"读取出错: {ex.Message}");
            }
        }

        private bool SaveToFile()
        {
            if (string.IsNullOrEmpty(_fullTxtPath))
            {
                ShowMessageDialog("提示", "未找到有效的名单路径！");
                return false;
            }

            try
            {
                var linesToSave = _students.Select(s => s.RawData).ToList();
                File.WriteAllLines(_fullTxtPath, linesToSave, Encoding.UTF8);

                // 名单落盘后让主窗口立即重新加载，无需重启程序
                Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.ReloadRoster();
                return true;
            }
            catch (Exception ex)
            {
                ShowMessageDialog("保存失败", ex.Message);
                return false;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
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
                    if (!cleanName.Contains("#")) cleanName += "#0";
                    _students.Add(new StudentItem { RawData = cleanName });
                    added = true;
                }
            }

            if (added)
            {
                InputTextBox.Clear();
                SaveToFile();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (StudentListView.SelectedItems == null || StudentListView.SelectedItems.Count == 0) return;

            var selected = StudentListView.SelectedItems.Cast<StudentItem>().ToList();
            foreach (var item in selected)
            {
                _students.Remove(item);
            }

            SaveToFile();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var host = GetDialogHost();
            if (host == null) return;

            var dialog = new ContentDialog(host)
            {
                Title = "警告",
                Content = "确定要清空名单吗？",
                PrimaryButtonText = "确定清空",
                CloseButtonText = "取消",
                PrimaryButtonAppearance = ControlAppearance.Danger
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _students.Clear();
                SaveToFile();
            }
        }
    }
}

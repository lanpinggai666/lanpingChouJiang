using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace WpfApp2
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        public string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public Window1()
        {
            InitializeComponent();
            string mindanPath = System.IO.Path.Combine(documentsPath, "mindan");

            // 确保目录存在
            if (!Directory.Exists(mindanPath))
            {
                Directory.CreateDirectory(mindanPath);
            }

            string path = System.IO.Path.Combine(mindanPath, "mindan.txt");

            // 如果名单文件不存在，创建空文件（后续操作会打开编辑器）
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "", Encoding.UTF8);
            }

            int lineCount = 0;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                while (sr.ReadLine() != null)
                {
                    lineCount++;
                }
            }

            // 如果 lineCount 为 0，Enumerable.Range(1,0) 会产生空序列，ComboBox 不会出错
            NumberComboBox.ItemsSource = Enumerable.Range(1, lineCount).ToList();
        }

        void OK(object sender, RoutedEventArgs e)
        {
            string mindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string path = System.IO.Path.Combine(mindanPath, "mindan.txt");
            string CountPath = System.IO.Path.Combine(mindanPath, "Count.txt");
            string AlreadyPath = System.IO.Path.Combine(mindanPath, "Already.txt");

            // 确保目录和文件存在
            if (!Directory.Exists(mindanPath))
            {
                Directory.CreateDirectory(mindanPath);
            }
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "", Encoding.UTF8);
                MessageBox.Show("检测到没有名单文件。已经自动创建文件。请在新创建的mindan.txt文件里输入名单，一行一个，不要有空格！",
                    "文件不存在",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Process process = new Process();
                process.StartInfo.FileName = "notepad.exe";
                process.StartInfo.Arguments = $"\"{path}\"";
                process.Start();
                return;
            }

            if (!File.Exists(AlreadyPath))
            {
                // 使用 "test" 占位以维持与原逻辑兼容
                ;
            }

            // 读取所有有效姓名（去掉空行并 Trim）
            var allLines = File.ReadAllLines(path, Encoding.UTF8)
                               .Select(s => s?.Trim())
                               .Where(s => !string.IsNullOrWhiteSpace(s))
                               .ToList();

            int studentsCount = allLines.Count;



            if (studentsCount == 0)
            {
                MessageBox.Show("名单文件是空的，请添加姓名后重新运行程序！",
                    "空文件",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Process process = new Process();
                process.StartInfo.FileName = "notepad.exe";
                process.StartInfo.Arguments = $"\"{path}\"";
                process.Start();
                return;
            }

            if (NumberComboBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择抽取数量。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int selectedValue = (int)NumberComboBox.SelectedItem;

            // 读取已抽名单，过滤空项与占位 "test"
            var alreadyLines = File.ReadAllLines(AlreadyPath, Encoding.UTF8)
                                   .Select(s => s?.Trim())
                                   .Where(s => !string.IsNullOrWhiteSpace(s))
                                   .ToHashSet(StringComparer.Ordinal);

            // 可选池为尚未抽过的姓名
            var available = allLines.Where(n => !alreadyLines.Contains(n)).ToList();

            bool wasReset = false;
            // 如果可选池小于需要抽取的数量，则重置已抽名单（与原逻辑一致）
            if (available.Count < selectedValue)
            {
                // 重置已抽文件并把可选池恢复为全部名单
                File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
                alreadyLines.Clear();
                available = new List<string>(allLines);
                wasReset = true;
            }

            // 防止请求数量超过名单总数
            if (selectedValue > allLines.Count)
            {
                selectedValue = allLines.Count;
            }

            // 使用 Fisher–Yates 洗牌选择不重复的姓名（更高效且无重复）
            var rng = new Random();
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = available[i];
                available[i] = available[j];
                available[j] = tmp;
            }

            var picked = available.Take(selectedValue).ToList();

            // 将选中的姓名追加到 Already.txt（每个姓名一行）
            // 保证文件以单独行保存
            var sbAppend = new StringBuilder();
            foreach (var name in picked)
            {
                sbAppend.AppendLine(name);
            }
            File.AppendAllText(AlreadyPath, sbAppend.ToString(), Encoding.UTF8);

            // 拼接显示结果
            string IsRestested = wasReset ? "已重置点名不重复\n" : string.Empty;
            string joined = string.Join(", ", picked);

            // MessageBox.Show($"{IsRestested}幸运儿： {joined} ({studentsCount})\n",
            //      "抽奖结果",
            //     MessageBoxButton.OK,
            //    MessageBoxImage.Information);
            Window3 w3 = new Window3();
            w3.NewTittle = "抽奖结果";
            w3.NewContent = $"幸运儿是：{joined}";
            w3.New_extra_text = $"";
            w3.ShowDialog();


            this.Close();
        }

        void Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
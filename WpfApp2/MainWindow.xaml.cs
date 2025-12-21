﻿using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui;

namespace lanpingcj
{
    public partial class MainWindow : Window
    {
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;

        // Win32 API 声明 - 新增的窗口样式相关常量和方法
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private bool Boy_isChecked { get; set; }
        public ICommand ExecuteActionCommand { get; private set; }
        public bool OnlyBoy = false;
        public bool OnlyGirl = false;
        public string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public double screenWidth = SystemParameters.PrimaryScreenWidth;
        public double screenHeight = SystemParameters.PrimaryScreenHeight;
        public bool AlreadyBe = true;
        public string Opened = string.Empty;
        public bool ShengWu = false;
        public bool gailv = false; // 新增：概率平衡开关
        private DispatcherTimer _clearTimer;
        public ContentDialogService _contentDialogService;

        public MainWindow()
        {
            int Width = 0;
            int Height = 0;
            InitializeComponent();
           
            // 设置不在任务栏显示
            ShowInTaskbar = false;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); // 设置时间间隔为1秒
            timer.Tick += Timer_Tick;
            timer.Start();

            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            // 检查文件夹是否存在
            if (!Directory.Exists(MindanPath))
            {
                // 如果不存在，创建文件夹
                Directory.CreateDirectory(MindanPath);
            }
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);

            Width = (int)Math.Ceiling(screenWidth - 60);
            Height = (int)Math.Floor((screenHeight - 120) / 2);
            //MessageBox.Show($"{Width},{Height}");
            this.Left = Width;
            this.Top = Height;

            _clearTimer = new DispatcherTimer();
            _clearTimer.Interval = TimeSpan.FromMinutes(25);
            _clearTimer.Tick += ClearAlreadyFile;

            //订阅 SourceInitialized 事件来设置窗口样式和置顶
            this.SourceInitialized += AWindow_SourceInitialized;

            // 订阅会话切换事件（处理锁屏）
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        // 新增：概率平衡开关方法
        void Switch_Probability_Balance(object sender, EventArgs e)
        {
            bool gailv = Properties.Settings.Default.gailv1;
            //gailv = !gailv;
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);

            if (gailv)
            {
                // 初始化概率平衡数据到名单文件
                InitializeProbabilityDataToFiles();

                Window3 w3 = new Window3();
                w3.NewTittle = "提示";
                w3.NewContent = "概率平衡功能已开启";
                w3.ShowDialog();
            }
            else
            {
                Window3 w3 = new Window3();
                w3.NewTittle = "提示";
                w3.NewContent = "概率平衡功能已关闭";
                w3.ShowDialog();
            }
        }

        // 新增：初始化概率平衡数据到名单文件
        private void InitializeProbabilityDataToFiles()
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");

            // 处理所有可能的名单文件
            string[] files = { "mindan.txt", "Boy_mindan.txt", "Girl_mindan.txt", "Shengwu_mindan.txt" };

            foreach (string file in files)
            {
                string filePath = System.IO.Path.Combine(MindanPath, file);
                if (File.Exists(filePath))
                {
                    // 读取文件内容，为每行添加计数标记
                    string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    List<string> updatedLines = new List<string>();

                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            // 如果行中已包含计数标记（格式：姓名#计数），则保留
                            if (line.Contains("#"))
                            {
                                // 确保计数格式正确
                                string[] parts = line.Split('#');
                                if (parts.Length == 2 && int.TryParse(parts[1], out _))
                                {
                                    updatedLines.Add(line);
                                }
                                else
                                {
                                    updatedLines.Add($"{line.Trim()}#0");
                                }
                            }
                            else
                            {
                                updatedLines.Add($"{line.Trim()}#0");
                            }
                        }
                    }

                    // 写回文件
                    if (updatedLines.Count > 0)
                    {
                        File.WriteAllLines(filePath, updatedLines, Encoding.UTF8);
                    }
                }
            }
        }

        // 新增：从名单文件中读取概率平衡数据
        private Dictionary<string, int> ReadProbabilityDataFromFile(string filePath)
        {
            Dictionary<string, int> records = new Dictionary<string, int>();

            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // 解析格式：姓名#计数
                        string[] parts = line.Split('#');
                        if (parts.Length == 2)
                        {
                            string name = parts[0].Trim();
                            if (int.TryParse(parts[1].Trim(), out int count))
                            {
                                records[name] = count;
                            }
                        }
                        else if (parts.Length == 1)
                        {
                            // 如果没有计数标记，默认为0
                            string name = parts[0].Trim();
                            records[name] = 0;
                        }
                    }
                }
            }

            return records;
        }

        // 新增：更新名单文件中的概率平衡数据
        private void UpdateProbabilityDataInFile(string filePath, string studentName, Dictionary<string, int> records)
        {
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                List<string> updatedLines = new List<string>();

                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string currentName = line.Split('#')[0].Trim();

                        if (currentName == studentName)
                        {
                            // 更新被选中的学生的计数
                            int newCount = records.ContainsKey(studentName) ? records[studentName] + 1 : 1;
                            updatedLines.Add($"{studentName}#{newCount}");
                        }
                        else
                        {
                            // 保持原样
                            updatedLines.Add(line);
                        }
                    }
                }

                // 写回文件
                File.WriteAllLines(filePath, updatedLines, Encoding.UTF8);
            }
        }

        // 新增：从名单文件中获取原始姓名列表（去除#计数部分）
        private List<string> GetOriginalNamesFromFile(string filePath)
        {
            List<string> names = new List<string>();

            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // 去除#及后面的计数部分
                        string[] parts = line.Split('#');
                        string name = parts[0].Trim();
                        if (!string.IsNullOrEmpty(name))
                        {
                            names.Add(name);
                        }
                    }
                }
            }

            return names;
        }

        // 新增：根据概率平衡选择学生
        private string SelectStudentWithProbabilityBalance(string filePath, List<string> originalNames, Dictionary<string, int> records)
        {
            if (originalNames.Count == 0) return null;

            // 计算每个学生的权重（被抽中次数越少，权重越高）
            List<(string name, double weight)> weightedList = new List<(string, double)>();

            foreach (string student in originalNames)
            {
                int count = records.ContainsKey(student) ? records[student] : 0;
                double weight = 1.0 / (count + 1); // 基础权重公式，可根据需要调整
                weightedList.Add((student, weight));
            }

            // 计算总权重
            double totalWeight = weightedList.Sum(item => item.weight);

            // 生成随机数
            Random random = new Random();
            double randomValue = random.NextDouble() * totalWeight;

            // 根据权重选择学生
            double cumulativeWeight = 0;
            foreach (var item in weightedList)
            {
                cumulativeWeight += item.weight;
                if (randomValue <= cumulativeWeight)
                {
                    return item.name;
                }
            }

            // 如果由于浮点数精度问题没有返回，返回最后一个
            return weightedList.Last().name;
        }

        // 新增的方法：设置窗口为工具窗口样式并置顶
        private void AWindow_SourceInitialized(object sender, EventArgs e)
        {
            // 获取窗口句柄
            var hwnd = new WindowInteropHelper(this).Handle;

            // 设置窗口扩展样式为工具窗口
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // 设置窗口为系统级置顶
            SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            // 同时设置WPF的Topmost属性作为辅助
            this.Topmost = true;
        }

        // 处理锁屏/解锁事件
        private void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                // 锁屏时取消置顶，避免遮挡登录界面
                case Microsoft.Win32.SessionSwitchReason.SessionLock:
                    this.Topmost = false;
                    break;
                // 解锁时恢复置顶
                case Microsoft.Win32.SessionSwitchReason.SessionUnlock:
                    this.Topmost = true;
                    // 重新设置系统级置顶
                    var hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    }
                    break;
            }
        }

        // 窗口激活状态变化时重新确保置顶
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // 确保窗口保持置顶状态
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        private void ClearAlreadyFile(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");

            try
            {
                File.Delete(AlreadyPath);
                File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
                _clearTimer.Stop(); // 执行一次后停止计时器               
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string? ReadSpecificLine(string filePath, int lineNumber)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                if (lineNumber >= 0 && lineNumber < lines.Length)
                {
                    return lines[lineNumber];
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"读取文件时出错: {ex.Message}");
                return null;
            }
        }

        void Open_mindan(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            Process process = new Process();
            process.StartInfo.FileName = "explorer.exe";
            process.StartInfo.Arguments = $"\"{MindanPath}\"";
            process.Start();
            return;
        }
        void test2(object sender, EventArgs e)
        {
            Window1 w1 = new Window1();
            // w8.Show(); // 打开一个非模态窗口，两个窗口都可以操作
            w1.ShowDialog(); // 打开一个模态窗口，只有Window8窗口可以操作，Window9窗口不可操作
        }

        void Only_Boy(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
            ShengWu = false;
            ShengwuName.Header = "开启生物特调";
            if (OnlyBoy == false)
            {
                if (OnlyGirl == true)
                {
                    OnlyGirl = false;
                    OnlyBoy = true;
                    Boy.Header = "取消只抽男的";
                    Girl.Header = "只抽女的";
                }
                else
                    OnlyBoy = true;
                Boy.Header = "取消只抽男的";
            }
            else
            {
                OnlyBoy = false;
                Boy.Header = "只抽男的";
            }
        }

        void Only_Girl(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
            ShengWu = false;
            ShengwuName.Header = "开启生物特调";
            if (OnlyGirl == false)
            {
                if (OnlyBoy == true)
                {
                    OnlyBoy = false;
                    OnlyGirl = true;
                    Boy.Header = "只抽男的";
                    Girl.Header = "取消只抽女的";
                }
                else
                    OnlyGirl = true;
                Girl.Header = "取消只抽女的";
            }
            else
            {
                OnlyGirl = false;
                Girl.Header = "只抽女的";
            }
        }

        void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            Window2 w2 = new Window2();
            // w8.Show(); // 打开一个非模态窗口，两个窗口都可以操作
            w2.Show();



        }
        void Shengwu(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
            if (OnlyBoy == false && OnlyGirl == false)
            {
                if (ShengWu == false)
                {
                    ShengWu = true;
                    ShengwuName.Header = "关闭生物特调";

                }
                else
                {
                    ShengWu = false;
                    ShengwuName.Header = "开启生物特调";
                }
            }
            else
            {
                OnlyBoy = false;
                OnlyGirl = false;
                Boy.Header = "只抽男的";
                Girl.Header = "只抽女的";
                if (ShengWu == false)
                {
                    ShengWu = true;
                    ShengwuName.Header = "关闭生物特调";
                }
                else
                {
                    ShengWu = false;
                    ShengwuName.Header = "开启生物特调";
                }

            }
        }

        void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            time.Text = DateTime.Now.ToString("HH:mm");
        }

        void button1_Click(object sender, EventArgs e)
        {
            int studentsCount;
            string mindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string path = System.IO.Path.Combine(mindanPath, "mindan.txt");
            string Boy_path = System.IO.Path.Combine(mindanPath, "Boy_mindan.txt");
            string Girl_path = System.IO.Path.Combine(mindanPath, "Girl_mindan.txt");
            string CountPath = System.IO.Path.Combine(mindanPath, "Count.txt");
            string AlreadyPath = System.IO.Path.Combine(mindanPath, "Already.txt");
            string ShengWu_path = System.IO.Path.Combine(mindanPath, "Shengwu_mindan.txt");
            bool gailv = Properties.Settings.Default.gailv1;

            int AlreadylineCount = 0;
            int AlreadyBeCount = 0;
            string AlreadyName = string.Empty;
            string BoyOrGirl = string.Empty;
            bool Duplicatea = Properties.Settings.Default.Duplicate;
            if (Duplicatea == true)
            {
                Opened = "已开启点名不重复";
            }
            else
            {
                Opened = "";
            }

            if (!Directory.Exists(mindanPath))
            {
                // 如果不存在，创建文件夹
                Directory.CreateDirectory(mindanPath);
            }
            if (OnlyBoy == true)
            {
                path = Boy_path;
                BoyOrGirl = "只抽男生\n";
            }
            if (OnlyGirl == true)
            {
                path = Girl_path;
                BoyOrGirl = "只抽女生\n";
            }
            if (ShengWu == true)
            {
                path = ShengWu_path;
                BoyOrGirl = "生物特调\n";
            }

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "", Encoding.UTF8);
                Window3 w3 = new Window3();
                w3.NewTittle = "提示";
                w3.NewContent = "未检测到名单文件，已经自动创建。";

                w3.ShowDialog();

                Process process = new Process();
                process.StartInfo.FileName = "notepad.exe";
                process.StartInfo.Arguments = $"\"{path}\"";
                process.Start();
                return;
            }

            int lineCount = 0;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                while (sr.ReadLine() != null)
                {
                    lineCount++;
                }
            }
            if (!File.Exists(AlreadyPath))
            {
                File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
            }
            using (StreamReader sr = new StreamReader(AlreadyPath, Encoding.UTF8))
            {
                while (sr.ReadLine() != null)
                {
                    AlreadylineCount++;
                }
            }

            studentsCount = lineCount;
            if (AlreadylineCount == lineCount)
            {
                File.Delete(AlreadyPath);
                File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
            }

            if (!File.Exists(CountPath))
            {
                File.WriteAllText(CountPath, studentsCount.ToString(), Encoding.UTF8);
                File.SetAttributes(CountPath, File.GetAttributes(CountPath) | FileAttributes.Hidden);
            }
            else
            {
                string content = File.ReadAllText(CountPath, Encoding.UTF8).Trim();
                int allstudentsCount = 0;
                allstudentsCount = int.Parse(content);
                if (allstudentsCount > studentsCount && OnlyBoy == false && OnlyGirl == false && ShengWu == false)
                {
                    File.SetAttributes(CountPath, File.GetAttributes(CountPath) & ~FileAttributes.Hidden);
                    File.WriteAllText(CountPath, studentsCount.ToString(), Encoding.UTF8);
                    File.SetAttributes(CountPath, File.GetAttributes(CountPath) | FileAttributes.Hidden);
                    int Truely_Students = 0;
                    Truely_Students = studentsCount++;
                    WarningMeassageBox w4 = new WarningMeassageBox();

                    w4.errorNewContent = $"发现名单学生数量减少！请检查名单！\n现在的学生数量: {Truely_Students}";


                    w4.ShowDialog();
                    Process process = new Process();
                    process.StartInfo.FileName = "notepad.exe";
                    process.StartInfo.Arguments = $"\"{path}\"";
                    process.Start();
                    return;
                }
                else
                {
                    File.SetAttributes(CountPath, File.GetAttributes(CountPath) & ~FileAttributes.Hidden);
                    File.WriteAllText(CountPath, studentsCount.ToString(), Encoding.UTF8);
                    File.SetAttributes(CountPath, File.GetAttributes(CountPath) | FileAttributes.Hidden);

                    if (studentsCount == 0)
                    {
                        WarningMeassageBox w4 = new WarningMeassageBox();

                        w4.errorNewContent = "名单文件是空的，请添加姓名后重新运行程序！ ";


                        w4.ShowDialog();
                        Process process = new Process();
                        process.StartInfo.FileName = "notepad.exe";
                        process.StartInfo.Arguments = $"\"{path}\"";
                        process.Start();
                        return;
                    }

                    string studentsName;
                    try
                    {
                        // 获取原始姓名列表（去除#计数部分）
                        List<string> originalNames = GetOriginalNamesFromFile(path);
                        int originalCount = originalNames.Count;

                        if (originalCount == 0)
                        {
                            WarningMeassageBox w4 = new WarningMeassageBox();
                            w4.errorNewContent = "名单文件是空的，请添加姓名后重新运行程序！";
                            w4.ShowDialog();
                            Process process = new Process();
                            process.StartInfo.FileName = "notepad.exe";
                            process.StartInfo.Arguments = $"\"{path}\"";
                            process.Start();
                            return;
                        }

                        // 新增：根据概率平衡开关选择学生
                        if (gailv)
                        {
                            // 读取概率平衡数据
                            Dictionary<string, int> probabilityRecords = ReadProbabilityDataFromFile(path);

                            // 根据概率平衡算法选择学生
                            studentsName = SelectStudentWithProbabilityBalance(path, originalNames, probabilityRecords);

                            if (string.IsNullOrEmpty(studentsName))
                            {
                                // 如果概率平衡选择失败，使用随机选择
                                Random random = new Random();
                                int randomLineNumber = random.Next(0, originalCount);
                                studentsName = originalNames[randomLineNumber];
                            }
                            else
                            {
                                // 更新名单文件中的概率平衡数据
                                UpdateProbabilityDataInFile(path, studentsName, probabilityRecords);
                            }
                        }
                        else
                        {
                            // 原随机选择逻辑（使用原始姓名列表）
                            Random random = new Random();
                            int randomLineNumber = random.Next(0, originalCount);
                            studentsName = originalNames[randomLineNumber];
                        }
                    }
                    catch (Exception ex)
                    {
                        WarningMeassageBox w4 = new WarningMeassageBox();

                        w4.errorNewContent = $"读取姓名时出现错误！请检查名单！\n错误信息: {ex.Message}";


                        w4.ShowDialog();

                        Process process = new Process();
                        process.StartInfo.FileName = "notepad.exe";
                        process.StartInfo.Arguments = $"\"{path}\"";
                        process.Start();
                        return;
                    }

                    if (string.IsNullOrEmpty(studentsName))
                    {
                        System.Windows.MessageBox.Show("读取到的姓名为空！请检查名单文件！",
                            "错误",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    using (StreamReader sr = new StreamReader(AlreadyPath, Encoding.UTF8))
                    {
                        while (sr.ReadLine() != null)
                        {
                            AlreadylineCount++;
                        }
                    }
                    bool nameIsUnique = false;
                    while (!nameIsUnique && Duplicatea == true)
                    {
                        nameIsUnique = true;

                        // 读取所有已抽过的名字
                        string[] alreadyLines = File.ReadAllLines(AlreadyPath, Encoding.UTF8);

                        // 检查当前随机到的名字是否在已抽名单中
                        for (int i = 0; i < alreadyLines.Length; i++)
                        {
                            if (alreadyLines[i] == studentsName)
                            {
                                nameIsUnique = false;
                                // 重新随机选择（使用原始姓名列表）
                                Random random = new Random();
                                List<string> originalNames = GetOriginalNamesFromFile(path);
                                int originalCount = originalNames.Count;
                                if (originalCount > 0)
                                {
                                    int randomLineNumber = random.Next(0, originalCount);
                                    studentsName = originalNames[randomLineNumber];
                                }
                                break;
                            }
                        }

                        // 添加安全机制，避免无限循环
                        if (AlreadyBeCount++ > lineCount)  // 设置最大尝试次数
                        {
                            //MessageBox.Show("尝试次数过多，可能所有学生都已被抽过");
                            break;
                        }
                    }

                    string IsRestested = string.Empty;
                    if (AlreadylineCount <= 2 && AlreadyBe == true)
                    {
                        IsRestested = "已重置点名不重复\n";
                    }

                    // 添加概率平衡状态显示
                    string probabilityStatus = gailv ? "概率平衡已开启\n" : "";

                    if (Properties.Settings.Default.tts == true)
                    {
                        Properties.Settings.Default.IsMain = true;
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        Properties.Settings.Default.IsMain = false;
                        Properties.Settings.Default.Save();

                    }

                    Window3 w3 = new Window3();
                    w3.NewTittle = "抽奖结果";
                    w3.NewContent = $"幸运儿是：{studentsName}";
                    w3.New_extra_text = $"{studentsCount}\n{BoyOrGirl}{IsRestested}{probabilityStatus}{Opened}";
                    w3.studentsName = $"{studentsName}";
                    w3.ShowDialog();


                    // 订阅事件




                    IsRestested = string.Empty;
                    if (AlreadyBe == true)
                    {
                        File.AppendAllText(AlreadyPath, "\r\n" + studentsName);
                    }
                    else
                    {
                        File.Delete(AlreadyPath);
                        File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
                    }
                    if (AlreadylineCount == 2)
                    {
                        _clearTimer.Stop(); // 先停止之前的计时器
                        _clearTimer.Start();
                    }

                }
            }
        }

        // 窗口关闭时取消事件订阅
        protected override void OnClosed(EventArgs e)
        {
            Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            base.OnClosed(e);
        }
    }
}

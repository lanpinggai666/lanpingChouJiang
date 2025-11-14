using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace WpfApp1
{
    public partial class AWindow : Window
    {
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private nint hWnd;
        private bool Boy_isChecked { get; set; }
        public ICommand ExecuteActionCommand { get; private set; }
        public bool OnlyBoy = false;
        public bool OnlyGirl = false;
        public string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public double screenWidth = SystemParameters.PrimaryScreenWidth;
        public double screenHeight = SystemParameters.PrimaryScreenHeight;
        public bool AlreadyBe = true;
        public string Opened = string.Empty;
        private DispatcherTimer _clearTimer;
        public AWindow()
        {
            int Width = 0;
            int Height = 0;
            InitializeComponent();
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
            if (AlreadyBe == true)
            {
                Opened = "已开启点名不重复";
            }
            _clearTimer = new DispatcherTimer();
            _clearTimer.Interval = TimeSpan.FromMinutes(15);
            _clearTimer.Tick += ClearAlreadyFile;
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

                // 可选：显示提示信息
                // MessageBox.Show("已清空抽奖记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"清空文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static string ReadSpecificLine(string filePath, int lineNumber)
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
                MessageBox.Show($"读取文件时出错: {ex.Message}");
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
        void Only_Boy(object sender, EventArgs e)
        {
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



        // 在主视图模型（如Window的DataContext）中，包含一个ObservableCollection<MenuItemModel>类型的TestItems属性[citation:6]。
        void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("作者：蓝屏钙，好喝的钙\nCV高级工程师\n版本：v1.0\n邮箱：hy12121@outlook.com\nGitHub：https://github.com/lanpinggai666\n软件开源，禁止倒卖！",
                "关于本软件",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        void CYN_YYDS(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("幸运儿是：蔡亚男",
                "抽奖结果",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        void StudentsIsChecked(object sender, RoutedEventArgs e)
        {
            if (AlreadyBe == true)
            {
                IsCheced.Header = "打开点名不重复";
                AlreadyBe = false;
                Opened = string.Empty;
            }
            else
            {
                IsCheced.Header = "关闭点名不重复";
                AlreadyBe = true;
                Opened = "已开启点名不重复";
            }
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
        }
        // 读取指定行的方法


        void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            time.Text = DateTime.Now.ToString("HH:mm");
        }
        void button1_Click(object sender, RoutedEventArgs e)
        {
            int studentsCount;
            string mindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string path = System.IO.Path.Combine(mindanPath, "mindan.txt");
            string Boy_path = System.IO.Path.Combine(mindanPath, "Boy_mindan.txt");
            string Girl_path = System.IO.Path.Combine(mindanPath, "Girl_mindan.txt");
            string CountPath = System.IO.Path.Combine(mindanPath, "Count.txt");
            string AlreadyPath = System.IO.Path.Combine(mindanPath, "Already.txt");
            int AlreadylineCount = 0;
            int AlreadyBeCount = 0;
            string AlreadyName = string.Empty;
            string BoyOrGirl = string.Empty;

            if (OnlyBoy == true)
            {
                path = Boy_path;
                BoyOrGirl = "(只抽男生)";
            }
            if (OnlyGirl == true)
            {
                path = Girl_path;
                BoyOrGirl = "(只抽女生)";
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

            int lineCount = 0;
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                while (sr.ReadLine() != null)
                {
                    lineCount++;
                }
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
                if (allstudentsCount > studentsCount && OnlyBoy == false && OnlyGirl == false)
                {
                    File.SetAttributes(CountPath, File.GetAttributes(CountPath) & ~FileAttributes.Hidden);
                    File.WriteAllText(CountPath, studentsCount.ToString(), Encoding.UTF8);
                    File.SetAttributes(CountPath, File.GetAttributes(CountPath) | FileAttributes.Hidden);
                    int Truely_Students = 0;
                    Truely_Students = studentsCount++;
                    MessageBox.Show($"发现名单学生数量减少！请检查名单！\n现在的学生数量: {Truely_Students}",
                        "警告",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
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

                    Random random = new Random();
                    int randomLineNumber = random.Next(0, studentsCount);

                    string studentsName;
                    try
                    {
                        string[] allLines = File.ReadAllLines(path, Encoding.UTF8);
                        studentsName = allLines[randomLineNumber];
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"读取姓名时出现错误！请检查名单！\n错误信息: {ex.Message}",
                            "错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        Process process = new Process();
                        process.StartInfo.FileName = "notepad.exe";
                        process.StartInfo.Arguments = $"\"{path}\"";
                        process.Start();
                        return;

                    }

                    if (string.IsNullOrEmpty(studentsName))
                    {
                        MessageBox.Show("读取到的姓名为空！请检查名单文件！",
                            "错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                        File.Delete(AlreadyPath);
                        File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
                    }
                    using (StreamReader sr = new StreamReader(AlreadyPath, Encoding.UTF8))
                    {
                        while (sr.ReadLine() != null)
                        {

                            AlreadylineCount++;
                        }
                    }
                    bool nameIsUnique = false;
                    while (!nameIsUnique && AlreadyBe == true)
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
                                // 重新随机选择
                                randomLineNumber = random.Next(0, studentsCount);
                                string[] allLines = File.ReadAllLines(path, Encoding.UTF8);
                                studentsName = allLines[randomLineNumber];
                                break;
                            }
                        }

                        // 添加安全机制，避免无限循环
                        if (AlreadyBeCount++ > 100)  // 设置最大尝试次数
                        {
                            //MessageBox.Show("尝试次数过多，可能所有学生都已被抽过");
                            break;
                        }
                    }






                    MessageBox.Show($"幸运儿是{BoyOrGirl}：{studentsName}({studentsCount})\n{Opened}",
                        "抽奖结果",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    if (AlreadyBe == true)
                    {
                        File.AppendAllText(AlreadyPath, "\r\n" + studentsName);
                    }
                    else
                    {
                        File.Delete(AlreadyPath);
                        File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);
                    }
                    if (AlreadylineCount == 1)
                    {
                        _clearTimer.Stop(); // 先停止之前的计时器
                        _clearTimer.Start();
                    }
                }
            }


        }
    }
}

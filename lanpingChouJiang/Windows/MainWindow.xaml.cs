using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Foundation.Collections;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;



namespace lanpingcj
{
    /// <summary>
    /// 主窗口类 - 实现点名抽奖功能
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 常量和Win32 API声明
        // 窗口置顶相关常量
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        // 鼠标操作标志
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002; // 鼠标左键按下
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;   // 鼠标左键抬起
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000; // 绝对坐标模式（基于屏幕分辨率）

        // 键盘操作常量
        private const int INPUT_KEYBOARD = 1;
        private const ushort KEYEVENTF_KEYDOWN = 0x0000;
        private const ushort KEYEVENTF_KEYUP = 0x0002;

        // 屏幕分辨率获取常量
        private const int SM_CXSCREEN = 0; // 屏幕宽度
        private const int SM_CYSCREEN = 1; // 屏幕高度



        // Win32 API声明 - 用于设置窗口样式
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // 窗口样式常量
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        // 1. 模拟鼠标输入（鼠标点击）
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        // 2. 发送键盘输入到活动窗口（跨窗口，不获取焦点）
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // 3. 获取屏幕分辨率（用于计算屏幕中间坐标）
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        // 4. 防止WPF窗口重新获取焦点 - 释放当前窗口焦点
        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);
        #endregion
        #region 结构体定义（SendInput 所需）
        // SendInput所需的输入结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type; // 输入类型：1=键盘，0=鼠标
            public INPUTUNION Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        // 键盘输入结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk; // 虚拟按键码
            public ushort wScan; // 扫描码
            public ushort dwFlags; // 按键标志
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // 鼠标输入结构体（本方案中仅备用，实际使用mouse_event）
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion

        #region 字段和属性
        // 命令执行接口（可能未设置）
        public ICommand? ExecuteActionCommand { get; private set; }

        // 是否只抽取男生的标志
        public bool OnlyBoy = false;

        // 是否只抽取女生的标志
        public bool OnlyGirl = false;

        // 文档路径，用于存储名单文件
        public string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // 屏幕宽度
        public double screenWidth = SystemParameters.PrimaryScreenWidth;

        // 屏幕高度
        public double screenHeight = SystemParameters.PrimaryScreenHeight;

        // 是否启用重复点名的标志
        public bool AlreadyBe = true;

        // 开启状态的文本
        public string Opened = string.Empty;

        // 是否启用生物特调的标志
        public bool ShengWu = false;

        // 清除定时器
        private DispatcherTimer _clearTimer;


        public ContentDialogService? _contentDialogService;
        #endregion

        public async Task<(string Version, string Mandatory)> GetVersion()
        {
            string url = "https://raw.githubusercontent.com/lanpinggai666/lanpingChouJiang/master/version";

            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            string content = await client.GetStringAsync(url);
            Debug.WriteLine($"原始内容: {content}");

            using StringReader reader = new StringReader(content);
            string version = reader.ReadLine()?.Trim() ?? string.Empty;
            string mandatory = reader.ReadLine()?.Trim() ?? string.Empty;

            // 一次性返回两个值
            return (version, mandatory);
        }

        // 调用时接收两个值
        public async Task CheckUpdate()
        {

            var result = await GetVersion();



            bool mandatory = bool.Parse(result.Mandatory);//强制更新
            Version LatestVersion = new Version(result.Version);
            Version ThisVersion = new Version(Properties.Settings.Default.ThisVersion);
            Debug.WriteLine($"当前版本: {ThisVersion}, 最新版本: {LatestVersion}, 强制更新: {mandatory}");
            if (LatestVersion > ThisVersion)
            {

                //  MoreInfo MoreInfo = new MoreInfo();
                //  MoreInfo.ShowDialog();
                // Need to dispatch to UI thread if performing UI operations


                await ShowSimpleToast();





            }




        }
        public async Task ShowSimpleToast()
        {
            new ToastContentBuilder()
                .AddText("新更新！")               // 主标题（加粗显示）
                .AddText("我们检测到了一个新的更新，点击这个通知以获取")             // 副标题（正常字体）
                .Show();                      // 立即显示
        }

        [Obsolete]
        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat args)
        {
            // 获取点击的按钮索引
            MoreInfo MoreInfo = new MoreInfo();
            MoreInfo.ShowDialog();
        }
        #region 构造函数
        /// <summary>
        /// 主窗口构造函数
        /// </summary>
        [Obsolete]
        public MainWindow()
        {
            int Width = 0;
            int Height = 0;
            InitializeComponent();
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;

                // Need to dispatch to UI thread if performing UI operations
                Dispatcher.InvokeAsync(delegate
                {
                    // TODO: Show the corresponding content
                    // MessageBox.Show("Toast activated. Args: " + toastArgs.Argument);
                    ToastNotificationManagerCompat_OnActivated(toastArgs);
                });
            };
            ShowInTaskbar = false;

            // 创建并启动时间更新定时器
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); // 设置时间间隔为1秒
            timer.Tick += Timer_Tick;
            timer.Start();

            // 创建名单文件夹
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            if (!Directory.Exists(MindanPath))
            {
                Directory.CreateDirectory(MindanPath);
            }

            // 初始化已抽取名单文件
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);

            // 设置窗口位置（屏幕右下角区域）
            Width = (int)Math.Ceiling(screenWidth - 60);
            Height = (int)Math.Floor((screenHeight - 120) / 2);
            this.Left = Width;
            this.Top = Height;

            // 创建并配置清除定时器（25分钟清除一次已抽取名单）
            _clearTimer = new DispatcherTimer();
            _clearTimer.Interval = TimeSpan.FromMinutes(25);
            _clearTimer.Tick += ClearAlreadyFile;

            // 订阅窗口初始化事件来设置窗口样式和置顶
            this.SourceInitialized += AWindow_SourceInitialized;
            this.KeyDown += MainWindow_KeyDown;

            // 订阅会话切换事件（处理锁屏）
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            this.Loaded += async (sender, e) =>
            {

                await CheckUpdate();
            };
            // Need to dispatch to UI thread if performing UI operations

        }
        #endregion
        #region 核心功能：WPF KeyDown事件处理
        /// <summary>
        /// WPF窗口有焦点时的按键捕获事件（核心入口）
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">按键事件参数</param>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // 1. 标记事件已处理，防止WPF窗口自身响应该按键（可选，根据需求调整）
            e.Handled = true;

            // 2. 将WPF的Key枚举转换为Windows虚拟按键码
            ushort virtualKeyCode = (ushort)KeyInterop.VirtualKeyFromKey(e.Key);
            if (virtualKeyCode == 0) // 过滤无效按键
                return;

            // 3. 执行核心逻辑：屏幕中间鼠标点击 + 跨窗口转发按键
            ExecuteKeyAndMouseCoreLogic(virtualKeyCode);
        }

        /// <summary>
        /// 核心业务逻辑：模拟屏幕中间鼠标点击 + 跨窗口转发按键 + 无焦点干扰
        /// </summary>
        /// <param name="virtualKeyCode">Windows虚拟按键码</param>
        private void ExecuteKeyAndMouseCoreLogic(ushort virtualKeyCode)
        {
            try
            {
                // 步骤1：计算屏幕中间坐标（绝对坐标）
                int screenWidth = GetSystemMetrics(SM_CXSCREEN); // 获取屏幕实际宽度
                int screenHeight = GetSystemMetrics(SM_CYSCREEN); // 获取屏幕实际高度
                int middleX = screenWidth / 2; // 屏幕水平中间
                int middleY = screenHeight / 2; // 屏幕垂直中间

                // 步骤2：模拟屏幕中间鼠标左键点击（完整点击：按下+抬起）
                // 转换为mouse_event要求的0-65535绝对坐标范围
                uint mouseX = (uint)(middleX * 65535 / screenWidth);
                uint mouseY = (uint)(middleY * 65535 / screenHeight);

                // 鼠标左键按下
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);
                // 鼠标左键抬起（必须成对调用，否则会出现鼠标长按状态）
                mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);

                // 步骤3：防止WPF窗口重新获取焦点 - 释放当前窗口焦点
                SetFocus(IntPtr.Zero); // 将焦点设置为空，不归属任何窗口

                // 步骤4：跨窗口转发按键（发送到系统当前活动窗口，不影响本WPF窗口）
                SendKeyToSystemActiveWindow(virtualKeyCode);
            }
            catch (Exception ex)
            {
                // MessageBox.Show($"操作失败：{ex.Message}", "错误提示", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 跨窗口发送按键（发送到系统当前活动窗口，本WPF窗口不重新获取焦点）
        /// </summary>
        /// <param name="virtualKeyCode">Windows虚拟按键码</param>
        private void SendKeyToSystemActiveWindow(ushort virtualKeyCode)
        {
            // 构建按键按下的输入数据
            INPUT keyDownInput = new INPUT
            {
                type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKeyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYDOWN,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // 构建按键抬起的输入数据（完整按键必须包含按下+抬起）
            INPUT keyUpInput = new INPUT
            {
                type = INPUT_KEYBOARD,
                Data = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKeyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // 组装并发送按键事件
            INPUT[] inputArray = new INPUT[] { keyDownInput, keyUpInput };
            SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf(typeof(INPUT)));
        }
        #endregion
        #region 概率平衡功能
        /// <summary>
        /// 切换概率平衡功能开关
        /// </summary>
        void Switch_Probability_Balance(object sender, EventArgs e)
        {
            bool isGailv = Properties.Settings.Default.gailv1;
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");

            // 清空已抽取名单
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);

            // 根据开关状态执行不同操作

        }

        /// <summary>
        /// 初始化概率平衡数据到名单文件
        /// </summary>
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

        /// <summary>
        /// 从名单文件中读取概率平衡数据
        /// </summary>
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

        /// <summary>
        /// 更新名单文件中的概率平衡数据
        /// </summary>
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

        /// <summary>
        /// 从名单文件中获取原始姓名列表（去除#计数部分）
        /// </summary>
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

        /// <summary>
        /// 根据概率平衡算法选择学生
        /// </summary>
        private string? SelectStudentWithProbabilityBalance(string filePath, List<string> originalNames, Dictionary<string, int> records)
        {
            if (originalNames.Count == 0) return null;

            // 计算每个学生的权重（被抽中次数越少，权重越高）
            List<(string name, double weight)> weightedList = new List<(string, double)>();

            foreach (string student in originalNames)
            {
                int count = records.ContainsKey(student) ? records[student] : 0;
                double weight = 1.0 / (count + 1); // 基础权重公式
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
            return weightedList.LastOrDefault().name;
        }
        #endregion

        #region 窗口管理
        /// <summary>
        /// 设置窗口为工具窗口样式并置顶
        /// </summary>
        private void AWindow_SourceInitialized(object? sender, EventArgs e)
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

        /// <summary>
        /// 处理锁屏/解锁事件
        /// </summary>
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

        /// <summary>
        /// 窗口激活状态变化时重新确保置顶
        /// </summary>
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
        #endregion

        #region 文件操作
        /// <summary>
        /// 清除已抽取名单文件
        /// </summary>
        private void ClearAlreadyFile(object? sender, EventArgs e)
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

                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 读取指定文件的指定行
        /// </summary>
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
        #endregion

        #region 菜单和按钮事件处理
        /// <summary>
        /// 打开名单文件夹
        /// </summary>
        void Open_mindan(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            Process process = new Process();
            process.StartInfo.FileName = "explorer.exe";
            process.StartInfo.Arguments = $"\"{MindanPath}\"";
            process.Start();
            return;
        }

        /// <summary>
        /// 打开测试窗口
        /// </summary>
        void Open_More_Man(object sender, EventArgs e)
        {
            ChoseMoreMan moreMan = new ChoseMoreMan();
            moreMan.ShowDialog(); // 打开模态窗口
        }

        /// <summary>
        /// 只抽取男生功能
        /// </summary>
        void Only_Boy(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");

            // 清空已抽取名单
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);

            // 关闭生物特调
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

        /// <summary>
        /// 只抽取女生功能
        /// </summary>
        void Only_Girl(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");

            // 清空已抽取名单
            File.Delete(AlreadyPath);
            File.WriteAllText(AlreadyPath, "test", Encoding.UTF8);

            // 关闭生物特调
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

        /// <summary>
        /// 关于菜单项点击事件
        /// </summary>
        [Obsolete]
        void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            MoreInfo more = new MoreInfo();
            more.Show(); // 打开关于窗口
        }

        /// <summary>
        /// 生物特调功能
        /// </summary>
        void Shengwu(object sender, EventArgs e)
        {
            string MindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyPath = System.IO.Path.Combine(MindanPath, "Already.txt");

            // 清空已抽取名单
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

        /// <summary>
        /// 退出菜单项点击事件
        /// </summary>
        void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// 时间更新定时器事件
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            time.Text = DateTime.Now.ToString("HH:mm");
        }

        /// <summary>
        /// 主要的抽奖按钮点击事件
        /// </summary>
        void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. 初始化路径和基本设置
                InitializePathsAndSettings(out var filePaths, out var settings);

                // 2. 确保文件夹和文件存在
                EnsureDirectoriesAndFilesExist(filePaths);

                // 3. 获取当前抽奖模式对应的名单文件路径
                string targetFilePath = GetTargetFilePath(filePaths);

                // 4. 检查名单文件是否存在，不存在则创建并提示用户
                if (!CheckAndCreateFileIfNotExist(targetFilePath))
                    return;

                // 5. 检查名单是否为空
                if (IsFileEmpty(targetFilePath))
                {
                    ShowEmptyFileWarning(targetFilePath);
                    return;
                }

                // 6. 处理名单人数检查逻辑
                if (!ProcessStudentCountCheck(targetFilePath, filePaths.CountFilePath))
                    return;

                // 7. 选择学生（考虑概率平衡）
                string? selectedStudent = SelectStudentFromFile(targetFilePath);
                if (string.IsNullOrEmpty(selectedStudent))
                {
                    ShowSelectionError(targetFilePath);
                    return;
                }

                // 8. 检查是否重复抽取（如果启用重复检查）
                if (settings.DuplicateCheckEnabled)
                {
                    selectedStudent = EnsureUniqueSelection(selectedStudent, targetFilePath,
                                                            filePaths.AlreadyFilePath);
                }

                // 9. 显示抽奖结果
                ShowSelectionResult(selectedStudent!, targetFilePath, settings);

                // 10. 更新已抽取记录
                UpdateAlreadyList(selectedStudent!, filePaths.AlreadyFilePath, settings.KeepAlreadyList);

                // 11. 如果这是第一次抽取，启动清除定时器
                StartClearTimerIfFirstSelection(filePaths.AlreadyFilePath);
            }
            catch (Exception ex)
            {
                HandleButtonClickError(ex);
            }
        }

        #region 辅助方法

        /// <summary>
        /// 初始化文件路径和设置
        /// </summary>
        private void InitializePathsAndSettings(out FilePaths filePaths, out ButtonClickSettings settings)
        {
            string mindanPath = System.IO.Path.Combine(documentsPath, "mindan");

            filePaths = new FilePaths
            {
                BasePath = System.IO.Path.Combine(mindanPath),
                DefaultFilePath = System.IO.Path.Combine(mindanPath, "mindan.txt"),
                BoyFilePath = System.IO.Path.Combine(mindanPath, "Boy_mindan.txt"),
                GirlFilePath = System.IO.Path.Combine(mindanPath, "Girl_mindan.txt"),
                CountFilePath = System.IO.Path.Combine(mindanPath, "Count.txt"),
                AlreadyFilePath = System.IO.Path.Combine(mindanPath, "Already.txt"),
                ShengWuFilePath = System.IO.Path.Combine(mindanPath, "Shengwu_mindan.txt")
            };

            settings = new ButtonClickSettings
            {
                ProbabilityBalanceEnabled = Properties.Settings.Default.gailv1,
                DuplicateCheckEnabled = Properties.Settings.Default.Duplicate,
                KeepAlreadyList = AlreadyBe,
                Opened = Properties.Settings.Default.Duplicate ? "已开启点名不重复" : ""
            };
        }

        /// <summary>
        /// 确保文件夹和文件存在
        /// </summary>
        private void EnsureDirectoriesAndFilesExist(FilePaths filePaths)
        {
            if (!Directory.Exists(filePaths.BasePath))
            {
                Directory.CreateDirectory(filePaths.BasePath);
            }
        }

        /// <summary>
        /// 获取当前抽奖模式对应的名单文件路径
        /// </summary>
        private string GetTargetFilePath(FilePaths filePaths)
        {
            if (OnlyBoy)
                return filePaths.BoyFilePath;
            if (OnlyGirl)
                return filePaths.GirlFilePath;
            if (ShengWu)
                return filePaths.ShengWuFilePath;

            return filePaths.DefaultFilePath;
        }

        /// <summary>
        /// 检查名单文件是否存在，不存在则创建并提示用户
        /// </summary>
        private bool CheckAndCreateFileIfNotExist(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "", Encoding.UTF8);


                string NewTittle = "提示";
                string NewContent = "未检测到名单文件，已经自动创建。";
                string New_extra_text = "";

                OpenMessageBox(NewTittle, NewContent, New_extra_text, "null");



                OpenFileInNotepad(filePath);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查文件是否为空
        /// </summary>
        private bool IsFileEmpty(string filePath)
        {
            var lines = File.ReadLines(filePath, Encoding.UTF8);
            return !lines.Any(line => !string.IsNullOrWhiteSpace(line));
        }

        /// <summary>
        /// 显示空文件警告
        /// </summary>
        private void ShowEmptyFileWarning(string filePath)
        {
            WarningMeassageBox w4 = new WarningMeassageBox
            {
                errorNewContent = "名单文件是空的，请添加姓名后重新运行程序！"
            };
            w4.ShowDialog();

            OpenFileInNotepad(filePath);
        }

        /// <summary>
        /// 处理名单人数检查逻辑
        /// </summary>
        private bool ProcessStudentCountCheck(string targetFilePath, string countFilePath)
        {
            int currentStudentCount = CountFileLines(targetFilePath);

            // 读取历史人数记录
            int previousStudentCount = ReadPreviousStudentCount(countFilePath);

            // 检查人数是否减少（仅在普通模式下）
            if (previousStudentCount > currentStudentCount &&
                OnlyBoy == false && OnlyGirl == false && ShengWu == false)
            {
                ShowStudentCountDecreaseWarning(currentStudentCount);
                OpenFileInNotepad(targetFilePath);
                return false;
            }

            // 更新人数记录
            UpdateStudentCountRecord(countFilePath, currentStudentCount);

            return true;
        }

        /// <summary>
        /// 计算文件行数
        /// </summary>
        private int CountFileLines(string filePath)
        {
            int count = 0;
            using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
            {
                while (sr.ReadLine() != null)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 读取之前记录的学生人数
        /// </summary>
        private int ReadPreviousStudentCount(string countFilePath)
        {
            if (!File.Exists(countFilePath))
            {
                return 0;
            }

            string content = File.ReadAllText(countFilePath, Encoding.UTF8).Trim();
            return int.TryParse(content, out int count) ? count : 0;
        }

        /// <summary>
        /// 显示学生人数减少的警告
        /// </summary>
        private void ShowStudentCountDecreaseWarning(int currentCount)
        {
            WarningMeassageBox w4 = new WarningMeassageBox
            {
                errorNewContent = $"发现名单学生数量减少！请检查名单！\n现在的学生数量: {currentCount}"
            };
            w4.ShowDialog();
        }

        /// <summary>
        /// 更新学生人数记录
        /// </summary>
        private void UpdateStudentCountRecord(string countFilePath, int studentCount)
        {
            if (File.Exists(countFilePath))
            {
                File.SetAttributes(countFilePath, File.GetAttributes(countFilePath) & ~FileAttributes.Hidden);
            }

            File.WriteAllText(countFilePath, studentCount.ToString(), Encoding.UTF8);
            File.SetAttributes(countFilePath, File.GetAttributes(countFilePath) | FileAttributes.Hidden);
        }

        /// <summary>
        /// 从文件中随机选择学生
        /// </summary>
        private string? SelectStudentFromFile(string filePath)
        {
            // 获取原始姓名列表
            List<string> originalNames = GetOriginalNamesFromFile(filePath);
            if (originalNames.Count == 0)
                return null;

            // 检查是否启用概率平衡
            if (Properties.Settings.Default.gailv1)
            {
                return SelectStudentWithProbabilityBalance(filePath, originalNames);
            }
            else
            {
                return SelectStudentRandomly(originalNames);
            }
        }

        /// <summary>
        /// 使用概率平衡算法选择学生
        /// </summary>
        private string? SelectStudentWithProbabilityBalance(string filePath, List<string> originalNames)
        {
            // 读取概率平衡数据
            Dictionary<string, int> probabilityRecords = ReadProbabilityDataFromFile(filePath);

            // 根据概率平衡算法选择学生
            string? selectedStudent = SelectStudentWithProbabilityBalance(filePath, originalNames, probabilityRecords);

            if (string.IsNullOrEmpty(selectedStudent))
            {
                // 如果概率平衡选择失败，使用随机选择
                selectedStudent = SelectStudentRandomly(originalNames);
            }
            else
            {
                // 更新名单文件中的概率平衡数据
                UpdateProbabilityDataInFile(filePath, selectedStudent, probabilityRecords);
            }

            return selectedStudent;
        }

        /// <summary>
        /// 随机选择学生
        /// </summary>
        private string SelectStudentRandomly(List<string> originalNames)
        {
            Random random = new Random();
            int randomIndex = random.Next(0, originalNames.Count);
            return originalNames[randomIndex];
        }

        /// <summary>
        /// 显示选择错误
        /// </summary>
        private void ShowSelectionError(string filePath)
        {
            System.Windows.MessageBox.Show("读取到的姓名为空！请检查名单文件！",
                "错误",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }

        /// <summary>
        /// 确保选择的姓名不重复（如果启用了重复检查）
        /// </summary>
        private string EnsureUniqueSelection(string selectedStudent, string sourceFilePath, string alreadyFilePath)
        {
            int maxAttempts = CountFileLines(sourceFilePath) * 2; // 最大尝试次数
            int attempts = 0;

            while (IsStudentAlreadySelected(selectedStudent, alreadyFilePath) && attempts < maxAttempts)
            {
                // 重新选择
                string? newSelection = SelectStudentFromFile(sourceFilePath);
                if (string.IsNullOrEmpty(newSelection))
                    break;
                selectedStudent = newSelection;
                attempts++;
            }

            return selectedStudent;
        }

        /// <summary>
        /// 检查学生是否已经被选中过
        /// </summary>
        private bool IsStudentAlreadySelected(string studentName, string alreadyFilePath)
        {
            if (!File.Exists(alreadyFilePath))
                return false;

            string[] alreadyLines = File.ReadAllLines(alreadyFilePath, Encoding.UTF8);
            return alreadyLines.Contains(studentName);
        }

        /// <summary>
        /// 显示抽奖结果
        /// </summary>
        private void ShowSelectionResult(string selectedStudent, string filePath, ButtonClickSettings settings)
        {
            // 构建显示信息
            string modeInfo = GetModeInfo();
            string duplicateInfo = GetDuplicateInfo(settings);

            // 设置TTS相关设置
            UpdateTtsSettings();

            // 显示结果窗口

            string NewTittle = "抽奖结果";
            string NewContent = $"幸运儿是：{selectedStudent}";
            string New_extra_text = $"{CountFileLines(filePath)}\n{modeInfo}{duplicateInfo}{settings.Opened}";
            string studentsName;
            studentsName = selectedStudent;
            OpenMessageBox(NewTittle,
                NewContent,
                New_extra_text
                , studentsName);
        }
        public void OpenMessageBox(string NewTittle, string NewContent, string New_extra_text, string studentsname)
        {

            MessageBox messageBox = new MessageBox();
            messageBox.NewTittle = NewTittle;
            messageBox.NewContent = NewContent;
            messageBox.New_extra_text = New_extra_text;
            messageBox.studentsName = studentsname;
            messageBox.ShowDialog();
        }

        /// <summary>
        /// 获取当前抽奖模式信息
        /// </summary>
        private string GetModeInfo()
        {
            if (OnlyBoy) return "只抽男生\n";
            if (OnlyGirl) return "只抽女生\n";
            if (ShengWu) return "生物特调\n";
            return "";
        }

        /// <summary>
        /// 获取重复点名信息
        /// </summary>
        private string GetDuplicateInfo(ButtonClickSettings settings)
        {
            string mindanPath = System.IO.Path.Combine(documentsPath, "mindan");
            string AlreadyFilePath = System.IO.Path.Combine(mindanPath, "Already.txt");

            if (CountFileLines(AlreadyFilePath) <= 1 && AlreadyBe == true)
            {
                return "已重置点名不重复\n";
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 更新TTS设置
        /// </summary>
        private void UpdateTtsSettings()
        {
            Properties.Settings.Default.IsMain = Properties.Settings.Default.tts;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 更新已抽取名单
        /// </summary>
        private void UpdateAlreadyList(string studentName, string alreadyFilePath, bool keepAlreadyList)
        {
            if (keepAlreadyList)
            {
                File.AppendAllText(alreadyFilePath, "\r\n" + studentName);
            }
            else
            {
                // 清空已抽取名单
                File.Delete(alreadyFilePath);
                File.WriteAllText(alreadyFilePath, "test", Encoding.UTF8);
            }
        }

        /// <summary>
        /// 如果这是第一次抽取，启动清除定时器
        /// </summary>
        private void StartClearTimerIfFirstSelection(string alreadyFilePath)
        {
            int alreadyLineCount = CountFileLines(alreadyFilePath);
            if (alreadyLineCount == 2) // 初始文件有1行"test"，加上第一次抽取的姓名
            {
                _clearTimer.Stop();
                _clearTimer.Start();
            }
        }

        /// <summary>
        /// 用记事本打开文件
        /// </summary>
        private void OpenFileInNotepad(string filePath)
        {
            Process.Start("notepad.exe", $"\"{filePath}\"");
        }

        /// <summary>
        /// 处理按钮点击过程中的异常
        /// </summary>
        private void HandleButtonClickError(Exception ex)
        {
            WarningMeassageBox w4 = new WarningMeassageBox
            {
                errorNewContent = $"操作过程中出现错误：{ex.Message}"
            };
            w4.ShowDialog();
        }

        #endregion

        #region 辅助数据结构

        /// <summary>
        /// 文件路径集合
        /// </summary>
        private class FilePaths
        {
            public string BasePath { get; set; } = string.Empty;
            public string DefaultFilePath { get; set; } = string.Empty;
            public string BoyFilePath { get; set; } = string.Empty;
            public string GirlFilePath { get; set; } = string.Empty;
            public string CountFilePath { get; set; } = string.Empty;
            public string AlreadyFilePath { get; set; } = string.Empty;
            public string ShengWuFilePath { get; set; } = string.Empty;
        }

        /// <summary>
        /// 按钮点击设置
        /// </summary>
        private class ButtonClickSettings
        {
            public bool ProbabilityBalanceEnabled { get; set; }
            public bool DuplicateCheckEnabled { get; set; }
            public bool KeepAlreadyList { get; set; }
            public string? Opened { get; set; }
        }
        #endregion

        #endregion

        #region 窗口生命周期
        /// <summary>
        /// 窗口关闭时取消事件订阅
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            base.OnClosed(e);
        }
    }
}
#endregion
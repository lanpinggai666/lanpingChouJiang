using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui;

namespace lanpingcj
{
    public partial class MainWindow : Window
    {
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int INPUT_KEYBOARD = 1;
        private const ushort KEYEVENTF_KEYDOWN = 0x0000;
        private const ushort KEYEVENTF_KEYUP = 0x0002;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        private static Mutex? _appMutex;
        private static bool _hasHandle = false;
        private DispatcherTimer? _updateTimer;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

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

        public string ConfigFilePath = Properties.Settings.Default.CurrentConfigFile ?? "config.json";
        public Config config = new Config();
        private List<string> _allNames = new List<string>();
        private HashSet<string> _alreadySelected = new HashSet<string>();
        private Dictionary<string, int> _nameCounts = new Dictionary<string, int>();
        private Random _random = new Random();
        public string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lanpingcj_mindan").Replace("\\", "\\\\");

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static bool IsAlreadyRunning()
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "lanpingcj";
            string mutexName = $"Global\\{assemblyName}_MainWindow_Mutex";
            try
            {
                bool createdNew;
                _appMutex = new Mutex(true, mutexName, out createdNew);
                _hasHandle = createdNew;
                if (!createdNew) return true;
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
                    string fallbackName = $"{assemblyName}_MainWindow_Mutex";
                    bool createdNew;
                    _appMutex = new Mutex(true, fallbackName, out createdNew);
                    _hasHandle = createdNew;
                    if (!createdNew) return true;
                }
                catch
                {
                    _hasHandle = false;
                }
            }
            catch
            {
                _hasHandle = false;
            }
            return false;
        }

        private static void ReleaseMutex()
        {
            if (_hasHandle && _appMutex != null)
            {
                try
                {
                    _appMutex.ReleaseMutex();
                }
                catch { }
                finally
                {
                    _appMutex.Dispose();
                    _appMutex = null;
                    _hasHandle = false;
                }
            }
        }

        public async Task<(string Version, string Mandatory)> GetVersion()
        {
            string url = "https://raw.githubusercontent.com/lanpinggai666/lanpingChouJiang/master/version";
            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            string content = await client.GetStringAsync(url);
            using StringReader reader = new StringReader(content);
            string version = reader.ReadLine()?.Trim() ?? string.Empty;
            string mandatory = reader.ReadLine()?.Trim() ?? string.Empty;
            return (version, mandatory);
        }

        public static void EnsurePreferExternalManifest()
        {
            const string subKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SideBySide";
            try
            {
                using RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey, true);
                if (key != null && key.GetValue("PreferExternalManifest")?.ToString() != "1")
                {
                    key.SetValue("PreferExternalManifest", 1, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        public async Task CheckUpdate()
        {
            var result = await GetVersion();
            bool mandatory = bool.TryParse(result.Mandatory, out bool m) && m;
            Version LatestVersion = new Version(result.Version);
            Version ThisVersion = new Version(Properties.Settings.Default.ThisVersion ?? "1.0.0.0");

            if (LatestVersion > ThisVersion)
            {
                await ShowSimpleToast("更新提醒", $"我们检测到了一个新的更新：{LatestVersion}，点击这个通知以获取更新", "OpenMoreInfo");
            }
        }

        public async Task ShowSimpleToast(string tittle, string text, string ToastAction)
        {
            new ToastContentBuilder()
                .AddArgument("action", ToastAction)
                .AddText(tittle)
                .AddText(text)
                .Show();
        }

        private void InitializeTimer()
        {
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMinutes(Properties.Settings.Default.Updatetime);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                await CheckUpdate();
                var result = await GetVersion();
                bool mandatory = bool.TryParse(result.Mandatory, out bool m) && m;
                if (mandatory)
                {
                    MoreInfo moreInfo = new MoreInfo();
                    moreInfo.Closed += (s, args) =>
                    {
                        Process.GetCurrentProcess().Kill();
                    };
                }
            }
            catch (Exception ex)
            {
                string error = $"错误: {ex.Message}\n请检查Internet连接和对Github的连通性。";
                await ShowSimpleToast("更新出错", error, "Download");
            }
        }

        public MainWindow()
        {
            EnsurePreferExternalManifest();
            if (IsAlreadyRunning())
            {
                Application.Current?.Shutdown();
                return;
            }

            this.Closed += (sender, e) =>
            {
                ReleaseMutex();
            };

            InitializeTimer();

            string configContent = @"{}";
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    configContent = File.ReadAllText(ConfigFilePath);
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch
            {
                string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, Properties.Settings.Default.defaultConfig ?? "default.json");
                if (File.Exists(defaultPath))
                {
                    configContent = File.ReadAllText(defaultPath);
                }
                else
                {
                    configContent = $@"{{""ConfigName"":""默认"",""mindan_path"":""default.txt"",""Repeat"":true,""Sound"":true,""TTS"":true,""Probability_balance"":true,""Lock"":false,""Lock_Password"":"""",""Use_StudentsID"":false,""Min_StudentsID"":1,""Max_StudentsID"":40,""Tittle"":""幸运儿""}}";
                    CheckFile(Path.Combine(folderPath, "default.txt"));
                }
                SendToastNotification("错误", "无法访问配置文件，已切换至默认配置文件！");
            }

            config = JsonSerializer.Deserialize<Config>(configContent, _jsonOptions) ?? new Config();
            InitializeComponent();
            LoadData();

            this.Loaded += async (sender, e) =>
            {
                SetWindowPositionToRight();
                AutoClickFocus(); // 窗口加载完毕后交还焦点给后台（如PPT）
                try
                {
                    await CheckUpdate();
                    var result = await GetVersion();
                    bool mandatory = bool.TryParse(result.Mandatory, out bool m) && m;
                    if (mandatory)
                    {
                        MoreInfo moreInfo = new MoreInfo();
                        moreInfo.Closed += (s, args) =>
                        {
                            Process.GetCurrentProcess().Kill();
                        };
                    }
                }
                catch (Exception ex)
                {
                    string error = $"错误: {ex.Message}\n请检查Internet连接和对Github的连通性。";
                    await ShowSimpleToast("更新出错", error, "Download");
                }
            };

            this.SourceInitialized += MainWindow_SourceInitialized;
            // 改为 PreviewKeyDown 确保即使焦点在按钮上也能截获键盘事件
            this.PreviewKeyDown += MainWindow_KeyDown;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        /// <summary>
        /// 自动点击屏幕中心，强制释放焦点给底层应用
        /// </summary>
        private void AutoClickFocus()
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
                        int middleX = screenWidth / 2;
                        int middleY = screenHeight / 2;

                        uint mouseX = (uint)(middleX * 65535 / screenWidth);
                        uint mouseY = (uint)(middleY * 65535 / screenHeight);

                        // 必须加 MOUSEEVENTF_MOVE 才能确保坐标生效
                        mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);
                        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);
                        mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);

                        SetFocus(IntPtr.Zero);
                    }
                    catch { }
                });
            });
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            ushort virtualKeyCode = (ushort)KeyInterop.VirtualKeyFromKey(e.Key);
            if (virtualKeyCode == 0) return;
            ExecuteKeyAndMouseCoreLogic(virtualKeyCode);
        }

        private void ExecuteKeyAndMouseCoreLogic(ushort virtualKeyCode)
        {
            try
            {
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);
                int middleX = screenWidth / 2;
                int middleY = screenHeight / 2;

                uint mouseX = (uint)(middleX * 65535 / screenWidth);
                uint mouseY = (uint)(middleY * 65535 / screenHeight);

                // 强制移动光标到屏幕中心再点击，确保点击事件能穿透到底层应用 (PPT)
                mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, mouseX, mouseY, 0, 0);

                SetFocus(IntPtr.Zero);
                SendKeyToSystemActiveWindow(virtualKeyCode);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void SendKeyToSystemActiveWindow(ushort virtualKeyCode)
        {
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

            INPUT[] inputArray = new INPUT[] { keyDownInput, keyUpInput };
            SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf(typeof(INPUT)));
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
            SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            this.Topmost = true;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    this.Topmost = false;
                    break;
                case SessionSwitchReason.SessionUnlock:
                    this.Topmost = true;
                    var hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    }
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            base.OnClosed(e);
        }

        private void LoadData()
        {
            if (string.IsNullOrEmpty(config?.mindan_path)) return;
            string mindanPath = Path.Combine(folderPath, config.mindan_path);
            CheckFile(mindanPath);
            if (!File.Exists(mindanPath)) return;

            _allNames.Clear();
            _nameCounts.Clear();

            var lines = File.ReadAllLines(mindanPath, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string name;
                int count = 0;

                if (line.Contains("#"))
                {
                    var parts = line.Split('#');
                    name = parts[0];
                    if (parts.Length > 1) int.TryParse(parts[1], out count);
                }
                else
                {
                    name = line;
                }

                _allNames.Add(name);
                _nameCounts[name] = count;
            }
        }

        private string GetRollCallResult(bool shouldExclude, bool isBalance)
        {
            if (_allNames.Count == 0) return string.Empty;

            var candidates = shouldExclude
                ? _allNames.Except(_alreadySelected).ToList()
                : _allNames.ToList();

            if (shouldExclude && candidates.Count == 0)
            {
                _alreadySelected.Clear();
                candidates = _allNames.ToList();
            }

            if (candidates.Count == 0) return string.Empty;

            string selectedName;

            if (isBalance && candidates.Count > 1)
            {
                int maxC = candidates.Max(n => _nameCounts.GetValueOrDefault(n, 0));
                var weightedList = candidates.Select(n => new {
                    Name = n,
                    Weight = (maxC - _nameCounts.GetValueOrDefault(n, 0) + 1)
                }).ToList();

                int totalWeight = weightedList.Sum(x => x.Weight);
                int dice = _random.Next(totalWeight);
                int cur = 0;
                selectedName = weightedList.First(x => (cur += x.Weight) > dice).Name;
            }
            else
            {
                selectedName = candidates[_random.Next(candidates.Count)];
            }

            return selectedName;
        }

        public void ResetData()
        {
            _alreadySelected.Clear();
        }

        private void SendToastNotification(string title, string content)
        {
            try
            {
                new ToastContentBuilder()
                    .AddText(title ?? string.Empty)
                    .AddText(content ?? string.Empty)
                    .Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void CheckFile(string filepath)
        {
            try
            {
                string? directory = Path.GetDirectoryName(filepath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SetWindowPositionToRight()
        {
            var workArea = SystemParameters.WorkArea;
            double windowWidth = this.ActualWidth > 0 ? this.ActualWidth : (double.IsNaN(this.Width) ? 300 : this.Width);
            double windowHeight = this.ActualHeight > 0 ? this.ActualHeight : (double.IsNaN(this.Height) ? 450 : this.Height);

            this.Left = workArea.Right - windowWidth;
            this.Top = workArea.Top + (workArea.Height - windowHeight) / 2;
        }

        private void SaveData()
        {
            if (string.IsNullOrEmpty(config?.mindan_path)) return;
            try
            {
                string mindanPath = Path.Combine(folderPath, config.mindan_path);
                var lines = _allNames.Select(name => $"{name}#{_nameCounts.GetValueOrDefault(name, 0)}");
                File.WriteAllLines(mindanPath, lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            time.Text = DateTime.Now.ToString("HH:mm");
        }

        void button1_Click(object sender, EventArgs e)
        {
            if (config == null) return;

            bool shouldExclude = !config.Repeat;
            bool isBalance = config.Probability_balance;
            string result = GetRollCallResult(shouldExclude, isBalance);

            if (result == string.Empty)
            {
                WarningMeassageBox error = new WarningMeassageBox();
                error.errorNewContent = "名单为空或配置错误，请前往名单管理查看！";
                error.ShowDialog();
            }
            else
            {
                if (shouldExclude)
                {
                    _alreadySelected.Add(result);
                }

                if (!_nameCounts.ContainsKey(result)) _nameCounts[result] = 0;
                _nameCounts[result]++;
                SaveData();

                string Tittle = config.Tittle ?? "幸运儿";
                string ConfigName = config.ConfigName ?? string.Empty;

                string IsRepeatStatusText = config.Repeat ? string.Empty : "已开启点名不重复！";

                string NewTittle = "抽奖结果";
                string NewContent = $"{Tittle}是：{result}";
                string New_extra_text = $"配置文件：{ConfigName}\n{IsRepeatStatusText}";
                OpenMessageBox(NewTittle, NewContent, New_extra_text, result);
            }
        }

        public static int GetLineCount(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return 0;
            }
            int lineCount = 0;
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (reader.ReadLine() != null)
                {
                    lineCount++;
                }
            }
            return lineCount;
        }

        void More_Click(object sender, EventArgs e)
        {
            new MoreInfo().Show();
            AutoClickFocus(); // 弹窗关闭后强制释放焦点
        }

        void MenuItem_Exit_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        void Open_mingdan(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(config?.mindan_path))
            {
                string mindanPath = Path.Combine(folderPath, config.mindan_path);
                string? dir = Path.GetDirectoryName(mindanPath);
                if (dir != null && Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", dir);
                }
            }
        }

        void Open_More_Man(object sender, EventArgs e)
        {
            new ChoseMoreMan().Show();
            AutoClickFocus(); // 弹窗关闭后强制释放焦点
        }

        public void OpenMessageBox(string NewTittle, string NewContent, string New_extra_text, string studentsname)
        {
            MessageBox messageBox = new MessageBox();
            messageBox.NewTittle = NewTittle;
            messageBox.NewContent = NewContent;
            messageBox.New_extra_text = New_extra_text;
            messageBox.studentsName = studentsname;
            messageBox.ShowDialog();
            AutoClickFocus(); 
        }
    }

    public class Config
    {
        public string mindan_path { get; set; } = string.Empty;
        public string ConfigName { get; set; } = string.Empty;
        public bool Repeat { get; set; }
        public bool Sound { get; set; }
        public bool TTS { get; set; }
        public bool Probability_balance { get; set; }
        public bool Lock { get; set; }
        public string Lock_Password { get; set; } = string.Empty;
        public bool Use_StudentsID { get; set; }
        public int Min_StudentsID { get; set; }
        public int Max_StudentsID { get; set; }
        public string Tittle { get; set; } = string.Empty;
    }
}
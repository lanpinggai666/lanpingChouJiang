using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        private DispatcherTimer? _clockTimer;

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

        public string ConfigFilePath = ConfigService.CurrentConfigPath;
        public Config config = new Config();
        private List<string> _allNames = new List<string>();
        private HashSet<string> _alreadySelected = new HashSet<string>();
        private Dictionary<string, int> _nameCounts = new Dictionary<string, int>();
        private Random _random = new Random();
        public string folderPath = ConfigService.MindanFolder;

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

        public static void EnsurePreferExternalManifest()
        {
            const string subKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SideBySide";
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(subKey, true);
                if (key != null && key.GetValue("PreferExternalManifest")?.ToString() != "1")
                {
                    key.SetValue("PreferExternalManifest", 1, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        public async Task CheckUpdateAsync()
        {
            var (latest, mandatory) = await UpdateService.GetLatestVersionAsync();
            if (latest <= UpdateService.CurrentVersion) return;

            if (mandatory)
            {
                // 强制更新：打开更新页面，关闭窗口即退出程序
                MoreInfo moreInfo = new MoreInfo { ToUpdatePage = true };
                moreInfo.Closed += (s, args) => Process.GetCurrentProcess().Kill();
                moreInfo.Show();
            }
            else
            {
                ShowToast("更新提醒", $"我们检测到了一个新的更新：{latest}，点击这个通知以获取更新", "OpenMoreInfo");
            }
        }

        private void ShowToast(string title, string text, string toastAction)
        {
            try
            {
                new ToastContentBuilder()
                    .AddArgument("action", toastAction)
                    .AddText(title ?? string.Empty)
                    .AddText(text ?? string.Empty)
                    .Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void InitializeTimer()
        {
            double interval = Properties.Settings.Default.Updatetime;
            if (interval <= 0) interval = 20;

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMinutes(interval);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                await CheckUpdateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查更新失败: {ex.Message}");
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

            string configContent;
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
                configContent = $@"{{""ConfigName"":""默认"",""mindan_path"":""default.txt"",""Repeat"":true,""Sound"":true,""TTS"":true,""Probability_balance"":true,""Lock"":false,""Lock_Password"":"""",""Use_StudentsID"":false,""Min_StudentsID"":1,""Max_StudentsID"":40,""Tittle"":""幸运儿""}}";
                CheckFile(Path.Combine(folderPath, "default.txt"));
                ShowToast("错误", "无法访问配置文件，已切换至默认配置文件！", "default");
            }

            config = JsonSerializer.Deserialize<Config>(configContent, ConfigService.JsonOptions) ?? new Config();
            InitializeComponent();
            LoadData();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += Timer_Tick;
            _clockTimer.Start();
            time.Text = DateTime.Now.ToString("HH:mm");

            this.Loaded += async (sender, e) =>
            {
                SetWindowPositionToRight();
                AutoClickFocus(); // 窗口加载完毕后交还焦点给后台（如PPT）
                try
                {
                    await CheckUpdateAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"检查更新失败: {ex.Message}");
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
            string? mindanPath = ConfigService.GetMindanFullPath(config);
            if (string.IsNullOrEmpty(mindanPath)) return;
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
                    name = parts[0].Trim();
                    if (parts.Length > 1) int.TryParse(parts[1], out count);
                }
                else
                {
                    name = line.Trim();
                }

                if (string.IsNullOrEmpty(name)) continue;
                _allNames.Add(name);
                _nameCounts[name] = count;
            }
        }

        /// <summary>
        /// 重新读取当前配置文件（供设置页在修改配置后调用，立即生效）
        /// </summary>
        public void RefreshConfig()
        {
            config = ConfigService.LoadCurrent();
        }

        /// <summary>
        /// 重新加载名单并清空"点名不重复"记录（供名单管理页调用，免重启生效）
        /// </summary>
        public void ReloadRoster()
        {
            RefreshConfig();
            LoadData();
            _alreadySelected.Clear();
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

        // 学号抽取模式：在 [Min_StudentsID, Max_StudentsID] 范围内随机抽取
        private string GetStudentIdResult(bool shouldExclude)
        {
            int min = Math.Min(config.Min_StudentsID, config.Max_StudentsID);
            int max = Math.Max(config.Min_StudentsID, config.Max_StudentsID);
            if (min < 1) min = 1;

            var allIds = Enumerable.Range(min, max - min + 1).Select(i => i.ToString()).ToList();
            var candidates = shouldExclude
                ? allIds.Where(id => !_alreadySelected.Contains(id)).ToList()
                : allIds;

            if (shouldExclude && candidates.Count == 0)
            {
                _alreadySelected.Clear();
                candidates = allIds;
            }

            if (candidates.Count == 0) return string.Empty;
            return candidates[_random.Next(candidates.Count)];
        }

        public void ResetData()
        {
            _alreadySelected.Clear();
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
            string? mindanPath = ConfigService.GetMindanFullPath(config);
            if (string.IsNullOrEmpty(mindanPath)) return;
            try
            {
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
            bool useStudentId = config.Use_StudentsID;

            string result = useStudentId
                ? GetStudentIdResult(shouldExclude)
                : GetRollCallResult(shouldExclude, config.Probability_balance);

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

                // 学号模式没有名单文件，不记录概率平衡计数
                if (!useStudentId)
                {
                    _nameCounts[result] = _nameCounts.GetValueOrDefault(result, 0) + 1;
                    SaveData();
                }

                string displayResult = useStudentId ? $"{result}号" : result;
                string Tittle = config.Tittle ?? "幸运儿";
                string ConfigName = config.ConfigName ?? string.Empty;

                string IsRepeatStatusText = config.Repeat ? string.Empty : "已开启点名不重复！";

                string NewTittle = "抽奖结果";
                string NewContent = $"{Tittle}是：{displayResult}";
                string New_extra_text = $"配置文件：{ConfigName}\n{IsRepeatStatusText}";
                OpenMessageBox(NewTittle, NewContent, New_extra_text, displayResult);
            }
        }

        void More_Click(object sender, EventArgs e)
        {
            MoreInfo.ShowUnique();
            AutoClickFocus(); // 弹窗关闭后强制释放焦点
        }

        void MenuItem_Exit_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        void Open_mingdan(object sender, EventArgs e)
        {
            string? mindanPath = ConfigService.GetMindanFullPath(config);
            if (!string.IsNullOrEmpty(mindanPath))
            {
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
}

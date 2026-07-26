using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace lanpingcj
{
    public class Config
    {
        public string ConfigName { get; set; } = string.Empty;
        public string mindan_path { get; set; } = string.Empty;
        public bool Repeat { get; set; } = true;
        public bool Sound { get; set; } = true;
        public bool TTS { get; set; } = true;
        public bool Probability_balance { get; set; } = true;
        public bool Lock { get; set; } = false;
        public string Lock_Password { get; set; } = string.Empty;
        public bool Use_StudentsID { get; set; } = false;
        public int Min_StudentsID { get; set; } = 1;
        public int Max_StudentsID { get; set; } = 40;
        public string Tittle { get; set; } = "幸运儿";
    }

    public static class ConfigService
    {
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string MindanFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lanpingcj_mindan");

        public static string DefaultConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Settings.Default.defaultConfig ?? "config.json");

        // 当前生效的配置文件路径；用户设置中的路径无效时回退到安装目录下的默认配置
        public static string CurrentConfigPath
        {
            get
            {
                string current = Properties.Settings.Default.CurrentConfigFile;
                if (string.IsNullOrEmpty(current) || !File.Exists(current))
                {
                    return DefaultConfigPath;
                }
                return current;
            }
        }

        public static Config LoadCurrent()
        {
            try
            {
                string path = CurrentConfigPath;
                if (!File.Exists(path)) return new Config();
                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<Config>(json, JsonOptions) ?? new Config();
            }
            catch
            {
                return new Config();
            }
        }

        public static void UpdateCurrent(Action<Config> updateAction)
        {
            try
            {
                string path = CurrentConfigPath;
                if (!File.Exists(path)) return;

                string json = File.ReadAllText(path, Encoding.UTF8);
                Config? config = JsonSerializer.Deserialize<Config>(json, JsonOptions);
                if (config == null) return;

                updateAction(config);
                File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions), Encoding.UTF8);
            }
            catch { }
        }

        public static string? GetMindanFullPath(Config? config)
        {
            if (string.IsNullOrEmpty(config?.mindan_path)) return null;
            return Path.IsPathRooted(config.mindan_path)
                ? config.mindan_path
                : Path.Combine(MindanFolder, config.mindan_path);
        }
    }
}

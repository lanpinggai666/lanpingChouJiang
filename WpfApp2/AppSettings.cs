using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace lanpingcj
{
    public static class AppSettings
    {
        private static string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "蓝屏抽奖机", "settings.json");

        public static bool SoundEnabled { get; set; } = true;

        static AppSettings()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<SettingsData>(json);
                    if (settings != null)
                    {
                        SoundEnabled = settings.SoundEnabled;
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果加载失败，使用默认值
                SoundEnabled = true;
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new SettingsData
                {
                    SoundEnabled = SoundEnabled
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        private class SettingsData
        {
            [JsonPropertyName("soundEnabled")]
            public bool SoundEnabled { get; set; } = true;
        }
    }
}
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace lanpingcj
{
    public static class UpdateService
    {
        public const string VersionUrl = "https://update.choujiang.lanpinggai.top/version";
        public const string DownloadUrl = "https://update.choujiang.lanpinggai.top/latest.exe";

        // 全局共享 HttpClient，避免每次检查更新都新建连接
        public static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            return client;
        }

        public static Version CurrentVersion
        {
            get
            {
                string ver = Properties.Settings.Default.ThisVersion;
                return Version.TryParse(ver, out var v) ? v : new Version(0, 0, 0);
            }
        }

        // 版本文件格式：第一行版本号，第二行是否强制更新（true/false）
        public static async Task<(Version Latest, bool Mandatory)> GetLatestVersionAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            string content = await Http.GetStringAsync(VersionUrl, cts.Token);

            using var reader = new StringReader(content);
            string versionLine = reader.ReadLine()?.Trim() ?? "0.0.0";
            string mandatoryLine = reader.ReadLine()?.Trim() ?? "false";

            Version latest = Version.TryParse(versionLine, out var v) ? v : new Version(0, 0, 0);
            bool mandatory = bool.TryParse(mandatoryLine, out bool m) && m;
            return (latest, mandatory);
        }
    }
}

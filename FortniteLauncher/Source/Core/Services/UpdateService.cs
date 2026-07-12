using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace FortniteLauncher.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string ReleaseUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
    }

    public static class UpdateService
    {
        // CHANGE THESE
        private const string GithubOwner = "ByZNexus";
        private const string GithubRepo = "Eon-Plus";

        private static readonly HttpClient _http = new HttpClient();

        static UpdateService()
        {
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EonPlusLauncher", "1.0"));
        }

        public static string GetCurrentVersionString()
        {
            var v = GetCurrentVersion();
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }

        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        }

        public static async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                var url = $"https://api.github.com/repos/{GithubOwner}/{GithubRepo}/releases/latest";
                var json = await _http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString() ?? "";
                var cleanTag = tagName.TrimStart('v', 'V');

                if (!Version.TryParse(cleanTag, out var remoteVersion))
                    return null;

                if (remoteVersion <= GetCurrentVersion())
                    return null; // up to date

                string htmlUrl = root.GetProperty("html_url").GetString() ??
                    $"https://github.com/{GithubOwner}/{GithubRepo}/releases/latest";
                string notes = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";

                return new UpdateInfo
                {
                    Version = cleanTag,
                    ReleaseUrl = htmlUrl,
                    ReleaseNotes = notes
                };
            }
            catch
            {
                return null; // fail silently, no update prompt on network error
            }
        }
    }
}
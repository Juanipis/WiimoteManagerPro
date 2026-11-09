using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WiimoteManager.Models;

namespace WiimoteManager.Services
{
    public class UpdateService
    {
        private const string GITHUB_REPO = "Juanipis/WiimoteManagerPro";
        private const string GITHUB_API_URL = "https://api.github.com/repos/Juanipis/WiimoteManagerPro/releases/latest";
        
        public async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                // GitHub API requires a User-Agent header
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WiimoteManager", "1.0"));
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetAsync(GITHUB_API_URL);
                if (!response.IsSuccessStatusCode) return;
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("tag_name", out var tagProp))
                {
                    // Remove 'v' prefix if present (e.g. v1.0.1 -> 1.0.1)
                    string latestTag = tagProp.GetString()?.TrimStart('v') ?? "";
                    string currentVersion = GetCurrentVersion();
                    
                    if (IsNewerVersion(latestTag, currentVersion))
                    {
                        var result = MessageBox.Show(
                            $"A new version ({latestTag}) is available!\n\nWould you like to download it now?",
                            "Update Available",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);
                            
                        if (result == MessageBoxResult.Yes)
                        {
                            if (root.TryGetProperty("html_url", out var urlProp))
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = urlProp.GetString(),
                                    UseShellExecute = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail updates check to not annoy user
            }
        }
        
        private string GetCurrentVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
        
        private bool IsNewerVersion(string latest, string current)
        {
            if (string.IsNullOrWhiteSpace(latest) || string.IsNullOrWhiteSpace(current)) return false;
            
            try
            {
                Version vLatest = new Version(latest);
                Version vCurrent = new Version(current);
                return vLatest > vCurrent;
            }
            catch
            {
                // Fallback to string comparison if parsing fails
                return string.Compare(latest, current) > 0;
            }
        }
    }
}

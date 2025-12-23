using System.Security.Principal;
using System.Diagnostics;

namespace PocketFence.Utils
{
    public static class SystemUtils
    {
        public static bool IsRunningAsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RestartAsAdministrator()
        {
            var exeName = Process.GetCurrentProcess().MainModule?.FileName;
            if (exeName != null)
            {
                var startInfo = new ProcessStartInfo(exeName)
                {
                    Verb = "runas"
                };
                
                try
                {
                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to restart as administrator: {ex.Message}");
                }
            }
        }

        public static bool CheckWindowsVersion()
        {
            var version = Environment.OSVersion.Version;
            
            // Windows 10 version 1607 (build 14393) or later required for hosted network
            if (version.Major >= 10 && version.Build >= 14393)
            {
                return true;
            }
            
            // Windows 7/8.1 also supported but with limited features
            if (version.Major >= 6)
            {
                return true;
            }
            
            return false;
        }

        public static async Task<bool> CheckWifiAdapterAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show drivers",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    return output.Contains("Hosted network supported") && 
                           output.Contains("Yes");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking WiFi adapter: {ex.Message}");
            }
            
            return false;
        }

        public static void CheckFirewallSettings()
        {
            Console.WriteLine("🔥 Firewall Check:");
            Console.WriteLine("   Please ensure Windows Firewall allows this application");
            Console.WriteLine("   to communicate through the network.");
            Console.WriteLine("   You may need to add firewall exceptions for:");
            Console.WriteLine("   • HTTP traffic (port 80, 8080)");
            Console.WriteLine("   • DNS traffic (port 53)");
            Console.WriteLine("   • PocketFence-Simple.exe");
        }

        public static string GetSystemInfo()
        {
            return $@"
System Information:
==================
OS: {Environment.OSVersion}
Machine: {Environment.MachineName}
User: {Environment.UserName}
.NET: {Environment.Version}
64-bit OS: {Environment.Is64BitOperatingSystem}
64-bit Process: {Environment.Is64BitProcess}
Admin Rights: {IsRunningAsAdministrator()}
";
        }

        public static void CreateLogDirectory()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        public static void SetupApplicationDirectories()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var directories = new[]
            {
                Path.Combine(baseDir, "logs"),
                Path.Combine(baseDir, "config"),
                Path.Combine(baseDir, "data")
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        public static async Task<string> GetPublicIpAddressAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetStringAsync("https://api.ipify.org");
                return response.Trim();
            }
            catch
            {
                return "Unable to determine";
            }
        }

        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double formattedBytes = bytes;
            
            while (formattedBytes >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                formattedBytes /= 1024;
                suffixIndex++;
            }
            
            return $"{formattedBytes:F2} {suffixes[suffixIndex]}";
        }

        public static void LogEvent(string message, string level = "INFO")
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] [{level}] {message}";
            
            Console.WriteLine(logMessage);
            
            try
            {
                var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "application.log");
                File.AppendAllTextAsync(logFile, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        public static bool IsValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

        public static bool IsValidMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                return false;
                
            // Remove common separators
            var cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(" ", "");
            
            // Should be exactly 12 hex characters
            return cleanMac.Length == 12 && 
                   cleanMac.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
        }
    }
}
using PocketFence_Simple.Interfaces;
using System.Security.Principal;
using System.Diagnostics;
#if WINDOWS
using System.Management;
#endif

namespace PocketFence_Simple.Platforms.Windows
{
    public class WindowsSystemUtilsService : ISystemUtilsService
    {
        public bool IsRunningAsAdministrator()
        {
#if WINDOWS
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
#else
            // For web applications, administrator checks are handled at the application level
            return false;
#endif
        }

        public void RestartAsAdministrator()
        {
#if WINDOWS
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var exeName = currentProcess.MainModule?.FileName;
                
                if (exeName != null)
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Verb = "runas",
                        UseShellExecute = true
                    };
                    
                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception for Windows platform
                Console.WriteLine($"Failed to restart as administrator: {ex.Message}");
            }
#else
            // For web applications, restart functionality is handled differently
            throw new NotSupportedException("Administrator restart is not supported in web applications");
#endif
        }

        public bool CheckSystemVersion()
        {
            var version = Environment.OSVersion.Version;
            
            // Windows 10 version 1607 (build 14393) or later required for hosted network
            if (version.Major >= 10 && version.Build >= 14393)
            {
                return true;
            }
            
            // Windows 7/8.1 also supported but with limited features
            return version.Major >= 6;
        }

        public async Task<bool> CheckWifiAdapterAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show drivers",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                        
                        var output = await process.StandardOutput.ReadToEndAsync();
                        
                        if (process.ExitCode == 0)
                        {
                            return output.Contains("Hosted network supported", StringComparison.OrdinalIgnoreCase) && 
                                   output.Contains("Yes", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        try { process.Kill(); } catch { }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error checking WiFi adapter: {ex.Message}", "ERROR");
            }
            
            return false;
        }

        public void SetupApplicationDirectories()
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

        public async Task<string> GetPublicIpAddressAsync()
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

        public string FormatBytes(long bytes)
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

        public void LogEvent(string message, string level = "INFO")
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] [{level}] {message}";
            
            System.Diagnostics.Debug.WriteLine(logMessage);
            
            try
            {
                var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "application.log");
                File.AppendAllTextAsync(logFile, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        public bool IsValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

        public bool IsValidMacAddress(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                return false;
                
            var cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(" ", "");
            
            return cleanMac.Length == 12 && 
                   cleanMac.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
        }

        public string GetSystemInfo()
        {
            return $@"System Information:
OS: {Environment.OSVersion}
Machine: {Environment.MachineName}
User: {Environment.UserName}
.NET: {Environment.Version}
64-bit OS: {Environment.Is64BitOperatingSystem}
64-bit Process: {Environment.Is64BitProcess}
Admin Rights: {IsRunningAsAdministrator()}";
        }
    }
}
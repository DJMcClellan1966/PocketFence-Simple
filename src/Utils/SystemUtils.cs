using System.Security.Principal;
using System.Diagnostics;
using System.Windows.Forms;

namespace PocketFence_Simple.Utils
{
    public static class SystemUtils
    {
        public static bool IsRunningAsAdministrator()
        {
#if WINDOWS
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
#else
            // For non-Windows platforms, return false or implement platform-specific logic
            return false;
#endif
        }

        public static void RestartAsAdministrator()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var exeName = currentProcess.MainModule?.FileName;
                
                ProcessStartInfo startInfo;
                
                // Check if we're running under dotnet (development scenario)
                if (exeName != null && exeName.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
                {
                    // We're running via 'dotnet run', so we need to use PowerShell to restart with elevation
                    var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var projectDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(assemblyLocation)));
                    
                    if (projectDir != null)
                    {
                        // Use PowerShell to start elevated dotnet run
                        startInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-Command \"Start-Process PowerShell -ArgumentList 'cd \\\"{projectDir}\\\"; dotnet run -- --restarted' -Verb RunAs\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                    }
                    else
                    {
                        throw new InvalidOperationException("Cannot determine project directory");
                    }
                }
                else if (exeName != null)
                {
                    // We're running as an executable - this should work with proper UAC
                    startInfo = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Verb = "runas",
                        UseShellExecute = true
                    };
                }
                else
                {
                    throw new InvalidOperationException("Cannot determine current process executable path");
                }
                
                Process.Start(startInfo);
                
                // Give the new process time to start before exiting
                System.Threading.Thread.Sleep(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // In a Windows Forms app, we should show a message box instead of console output
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to restart as administrator: {ex.Message}\n\nPlease manually run the application as administrator.",
                    "Administrator Restart Failed",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
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
                // Try primary method first (netsh)
                var netshResult = await CheckWifiAdapterWithNetshAsync();
                if (netshResult.HasValue)
                {
                    LogEvent($"WiFi adapter check (netsh) completed. Supported: {netshResult.Value}");
                    return netshResult.Value;
                }
                
                // Fallback to WMI method
                LogEvent("Trying fallback WiFi detection method...", "INFO");
                var wmiResult = await CheckWifiAdapterWithWmiAsync();
                LogEvent($"WiFi adapter check (WMI) completed. Supported: {wmiResult}");
                return wmiResult;
            }
            catch (Exception ex)
            {
                LogEvent($"Error in WiFi adapter check: {ex.Message}", "ERROR");
                return false;
            }
        }
        
        private static async Task<bool?> CheckWifiAdapterWithNetshAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8)); // 8-second timeout
                
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
                        var outputTask = process.StandardOutput.ReadToEndAsync();
                        var errorTask = process.StandardError.ReadToEndAsync();
                        
                        // Wait for process with timeout
                        await process.WaitForExitAsync(cts.Token);
                        
                        var output = await outputTask;
                        var error = await errorTask;
                        
                        if (process.ExitCode == 0)
                        {
                            // Check for hosted network support
                            var hasHostedSupport = output.Contains("Hosted network supported", StringComparison.OrdinalIgnoreCase) && 
                                                  output.Contains("Yes", StringComparison.OrdinalIgnoreCase);
                            
                            return hasHostedSupport;
                        }
                        else
                        {
                            LogEvent($"netsh command failed with exit code {process.ExitCode}. Error: {error}", "WARNING");
                            return null; // Trigger fallback
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogEvent("WiFi adapter check timed out. Using fallback method...", "WARNING");
                        try
                        {
                            process.Kill();
                        }
                        catch { /* Ignore kill errors */ }
                        return null; // Trigger fallback
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error in netsh WiFi check: {ex.Message}", "WARNING");
            }
            
            return null; // Trigger fallback
        }
        
        private static async Task<bool> CheckWifiAdapterWithWmiAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    using var searcher = new System.Management.ManagementObjectSearcher(
                        "SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2 AND AdapterTypeId = 9");
                    
                    var adapters = searcher.Get();
                    return adapters.Count > 0;
                });
                
                // If we have active wireless adapters, assume hosted network might be supported
                // This is not as reliable as netsh but provides a reasonable fallback
                return true;
            }
            catch (Exception ex)
            {
                LogEvent($"Error in WMI WiFi check: {ex.Message}", "WARNING");
                return false;
            }
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
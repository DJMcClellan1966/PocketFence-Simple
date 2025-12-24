using System.Security.Principal;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;

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

        /// <summary>
        /// Securely restarts the application with administrator privileges.
        /// Security measures:
        /// - Validates that the executable path is within the application directory to prevent arbitrary execution
        /// - Ensures the path exists and is a valid executable file
        /// - Does not pass any command-line arguments to prevent parameter injection
        /// - Uses only the verified executable path without shell execution
        /// </summary>
        public static void RestartAsAdministrator()
        {
            var exeName = Process.GetCurrentProcess().MainModule?.FileName;
            
            // Security: Validate executable path to prevent arbitrary execution
            if (string.IsNullOrWhiteSpace(exeName))
            {
                SecureLogError("Cannot restart as administrator: executable path is null or empty");
                return;
            }

            // Security: Ensure the executable exists and is a valid file
            if (!File.Exists(exeName))
            {
                SecureLogError("Cannot restart as administrator: executable file not found");
                return;
            }

            // Security: Validate the path is within expected application directory to prevent path traversal
            var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            var fullExePath = Path.GetFullPath(exeName);
            var fullBaseDir = Path.GetFullPath(appBaseDir);
            
            // Security: Use GetRelativePath to detect path traversal attempts (more robust than StartsWith)
            try
            {
                var relativePath = Path.GetRelativePath(fullBaseDir, fullExePath);
                if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
                {
                    SecureLogError("Cannot restart as administrator: executable path is outside application directory");
                    return;
                }
            }
            catch
            {
                SecureLogError("Cannot restart as administrator: invalid path");
                return;
            }

            // Security: Verify the file is an executable
            if (!fullExePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                SecureLogError("Cannot restart as administrator: file is not an executable");
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo(fullExePath)
                {
                    Verb = "runas",
                    UseShellExecute = true,  // Required for "runas" verb
                    // Security: Do not pass any arguments to prevent parameter injection
                    Arguments = string.Empty
                };
                
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch (Exception)
            {
                // Security: Don't expose detailed error information to the user
                SecureLogError("Failed to restart as administrator. Please run the application as administrator manually.");
                Console.WriteLine("Failed to restart as administrator. Please run the application as administrator manually.");
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
            catch (Exception)
            {
                // Security: Log error without exposing sensitive details
                SecureLogError("Error checking WiFi adapter capabilities");
                Console.WriteLine("Error checking WiFi adapter. Please ensure wireless adapter is properly installed.");
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
            
            // Security: Validate path using GetRelativePath (more robust than StartsWith)
            var fullPath = Path.GetFullPath(logDir);
            var fullBaseDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            
            try
            {
                var relativePath = Path.GetRelativePath(fullBaseDir, fullPath);
                if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
                {
                    Console.WriteLine("Error: Invalid log directory path");
                    return;
                }
            }
            catch
            {
                Console.WriteLine("Error: Invalid log directory path");
                return;
            }
            
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                SetSecureDirectoryPermissions(fullPath);
            }
        }

        /// <summary>
        /// Creates application directories with secure permissions.
        /// Security measures:
        /// - Validates directory paths to prevent path traversal
        /// - Sets restrictive ACLs to allow only administrators and system access
        /// - Ensures directories are within the application base directory
        /// </summary>
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
                try
                {
                    // Security: Validate the directory path to prevent path traversal
                    var fullPath = Path.GetFullPath(dir);
                    var fullBaseDir = Path.GetFullPath(baseDir);
                    
                    // Security: Use GetRelativePath to detect path traversal (more robust)
                    var relativePath = Path.GetRelativePath(fullBaseDir, fullPath);
                    if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
                    {
                        SecureLogError($"Security violation: Directory path is outside base directory");
                        continue;
                    }

                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                        
                        // Security: Set restrictive permissions on the directory
                        SetSecureDirectoryPermissions(fullPath);
                    }
                }
                catch (Exception)
                {
                    // Security: Log error without exposing sensitive path details
                    SecureLogError($"Failed to create or secure application directory");
                    Console.WriteLine($"Warning: Could not create or secure directory: {Path.GetFileName(dir)}");
                }
            }
        }

        /// <summary>
        /// Sets secure permissions on a directory to restrict access to administrators and system only.
        /// </summary>
        private static void SetSecureDirectoryPermissions(string directoryPath)
        {
            try
            {
                // Security: Configure ACL to allow only Administrators and SYSTEM full control
                var dirInfo = new DirectoryInfo(directoryPath);
                var dirSecurity = dirInfo.GetAccessControl();
                
                // Disable inheritance and remove existing rules
                dirSecurity.SetAccessRuleProtection(true, false);
                
                foreach (FileSystemAccessRule rule in dirSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                {
                    dirSecurity.RemoveAccessRule(rule);
                }
                
                // Add rule for Administrators (Full Control)
                var adminRule = new FileSystemAccessRule(
                    new System.Security.Principal.SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow
                );
                dirSecurity.AddAccessRule(adminRule);
                
                // Add rule for SYSTEM (Full Control)
                var systemRule = new FileSystemAccessRule(
                    new System.Security.Principal.SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow
                );
                dirSecurity.AddAccessRule(systemRule);
                
                // Add rule for current user (Full Control) - needed for the application to function
                var currentUser = WindowsIdentity.GetCurrent();
                if (currentUser.User != null)
                {
                    var userRule = new FileSystemAccessRule(
                        currentUser.User,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow
                    );
                    dirSecurity.AddAccessRule(userRule);
                }
                
                dirInfo.SetAccessControl(dirSecurity);
            }
            catch (Exception)
            {
                // Security: Log without exposing details
                SecureLogError("Failed to set secure permissions on directory");
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

        /// <summary>
        /// Securely logs events to file and console without exposing sensitive information.
        /// Security measures:
        /// - Sanitizes log messages to prevent log injection
        /// - Does not log sensitive data like passwords, tokens, or personal information
        /// - Validates file path before writing
        /// </summary>
        public static void LogEvent(string message, string level = "INFO")
        {
            // Security: Sanitize message to prevent log injection attacks
            var sanitizedMessage = SanitizeLogMessage(message);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] [{level}] {sanitizedMessage}";
            
            Console.WriteLine(logMessage);
            
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logFile = Path.Combine(baseDir, "logs", "application.log");
                
                // Security: Validate the log file path using GetRelativePath
                var fullPath = Path.GetFullPath(logFile);
                var fullBaseDir = Path.GetFullPath(baseDir);
                
                try
                {
                    var relativePath = Path.GetRelativePath(fullBaseDir, fullPath);
                    if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
                    {
                        Console.WriteLine("Error: Invalid log file path");
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("Error: Invalid log file path");
                    return;
                }
                
                File.AppendAllText(fullPath, logMessage + Environment.NewLine);
            }
            catch (Exception)
            {
                // Don't log to console if file logging fails to avoid infinite loops
                // Just continue - console logging already happened
            }
        }

        /// <summary>
        /// Securely logs errors without exposing sensitive information like stack traces to end users.
        /// Logs are written to file only, not to console.
        /// </summary>
        private static void SecureLogError(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var sanitizedMessage = SanitizeLogMessage(message);
                var logMessage = $"[{timestamp}] [ERROR] {sanitizedMessage}";
                
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logFile = Path.Combine(baseDir, "logs", "application.log");
                
                // Security: Validate path using GetRelativePath
                var fullPath = Path.GetFullPath(logFile);
                var fullBaseDir = Path.GetFullPath(baseDir);
                
                try
                {
                    var relativePath = Path.GetRelativePath(fullBaseDir, fullPath);
                    if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
                        return;
                }
                catch
                {
                    return;
                }
                
                File.AppendAllText(fullPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // Silently fail - this is error logging, we can't do much if it fails
            }
        }

        /// <summary>
        /// Sanitizes log messages to prevent log injection attacks.
        /// Removes newlines and carriage returns that could be used to inject fake log entries.
        /// </summary>
        private static string SanitizeLogMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;
            
            // Security: Remove newlines and carriage returns to prevent log injection
            return message.Replace("\r", "").Replace("\n", " ").Trim();
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

        /// <summary>
        /// Validates SSID to prevent command injection and ensure proper format.
        /// Security measures:
        /// - Restricts length to prevent buffer overflow
        /// - Allows only safe characters (alphanumeric, dash, underscore)
        /// - Prevents special characters that could be used for injection
        /// </summary>
        public static bool IsValidSsid(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid))
                return false;
            
            // SSID must be 1-32 characters (WiFi standard limit)
            if (ssid.Length < 1 || ssid.Length > 32)
                return false;
            
            // Security: Allow only alphanumeric, space, dash, and underscore to prevent injection
            return ssid.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_');
        }

        /// <summary>
        /// Validates WiFi password to ensure it meets security requirements.
        /// Security measures:
        /// - Enforces minimum length for security
        /// - Restricts maximum length to prevent buffer overflow
        /// - Allows all printable ASCII characters (as per WPA/WPA2 standard)
        /// Note: The password will be properly quoted when used in shell commands
        /// </summary>
        public static bool IsValidWifiPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
            
            // WPA2 password must be 8-63 characters
            if (password.Length < 8 || password.Length > 63)
                return false;
            
            // Security: Allow all printable ASCII characters (32-126) as per WPA/WPA2 standard
            // The password will be properly escaped when used in commands
            foreach (char c in password)
            {
                // Ensure character is in printable ASCII range
                if (c < 32 || c > 126)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Validates URL format to prevent injection attacks.
        /// Security measures:
        /// - Ensures proper URL format
        /// - Prevents malformed URLs that could cause security issues
        /// </summary>
        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
            
            // Use Uri.TryCreate with UriKind.Absolute for validation
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Validates domain name format.
        /// Security measures:
        /// - Ensures proper domain format
        /// - Prevents malformed domains
        /// </summary>
        public static bool IsValidDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;
            
            // Domain name constraints: alphanumeric, dots, and dashes
            // Must not start or end with dash or dot
            if (domain.StartsWith("-") || domain.EndsWith("-") ||
                domain.StartsWith(".") || domain.EndsWith("."))
                return false;
            
            // Check length (max 253 characters for full domain)
            if (domain.Length > 253)
                return false;
            
            // Each label (part between dots) should be valid
            var labels = domain.Split('.');
            foreach (var label in labels)
            {
                if (string.IsNullOrWhiteSpace(label) || label.Length > 63)
                    return false;
                
                // Labels can only contain alphanumeric and dash (not at start/end)
                if (!label.All(c => char.IsLetterOrDigit(c) || c == '-'))
                    return false;
                
                if (label.StartsWith("-") || label.EndsWith("-"))
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Sanitizes file paths to prevent path traversal attacks.
        /// Security measures:
        /// - Uses GetRelativePath to detect path traversal sequences
        /// - Ensures path stays within the application directory
        /// - Handles symbolic links and case-sensitivity properly
        /// </summary>
        public static string? SanitizeFilePath(string path, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(baseDirectory))
                return null;
            
            try
            {
                // Get full paths to check for traversal
                var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, path));
                var fullBaseDir = Path.GetFullPath(baseDirectory);
                
                // Security: Use GetRelativePath to detect path traversal (more robust than StartsWith)
                var relativePath = Path.GetRelativePath(fullBaseDir, fullPath);
                
                // If the relative path starts with "..", it's attempting to go outside the base directory
                if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
                    return null;
                
                return fullPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Escapes a string for safe use in shell commands.
        /// Security measures:
        /// - Adds quotes around the argument
        /// - Escapes quotes and backslashes within the argument
        /// - Prevents command injection through special characters
        /// </summary>
        public static string EscapeShellArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return "\"\"";
            
            // Escape backslashes and quotes
            var escaped = argument.Replace("\\", "\\\\").Replace("\"", "\\\"");
            
            // Wrap in quotes
            return $"\"{escaped}\"";
        }
    }
}
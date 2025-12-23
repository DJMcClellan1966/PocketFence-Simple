using PocketFence.Services;
using PocketFence.Models;

namespace PocketFence.UI
{
    public class ConsoleUI
    {
        private readonly HotspotService _hotspotService;
        private readonly ContentFilterService _contentFilterService;
        private readonly NetworkTrafficService _networkTrafficService;
        private readonly List<ConnectedDevice> _connectedDevices;
        
        public ConsoleUI()
        {
            _hotspotService = new HotspotService();
            _contentFilterService = new ContentFilterService();
            _networkTrafficService = new NetworkTrafficService(_contentFilterService);
            _connectedDevices = new List<ConnectedDevice>();
            
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            _hotspotService.HotspotStatusChanged += (sender, message) => 
            {
                Console.WriteLine($"[HOTSPOT] {message}");
            };
            
            _hotspotService.DeviceConnected += (sender, device) => 
            {
                _connectedDevices.Add(device);
                Console.WriteLine($"[DEVICE] Connected: {device.DeviceName} ({device.IpAddress})");
            };
            
            _hotspotService.DeviceDisconnected += (sender, device) => 
            {
                var existingDevice = _connectedDevices.FirstOrDefault(d => d.MacAddress == device.MacAddress);
                if (existingDevice != null)
                {
                    _connectedDevices.Remove(existingDevice);
                    Console.WriteLine($"[DEVICE] Disconnected: {device.DeviceName} ({device.IpAddress})");
                }
            };
            
            _contentFilterService.SiteBlocked += (sender, blockedSite) => 
            {
                Console.WriteLine($"[BLOCKED] {blockedSite.Url} - {blockedSite.Reason} (Device: {blockedSite.DeviceMac})");
            };
            
            _networkTrafficService.TrafficMonitoringStatusChanged += (sender, message) => 
            {
                Console.WriteLine($"[TRAFFIC] {message}");
            };
        }

        public async Task RunAsync()
        {
            Console.WriteLine("🛡️  PocketFence-Simple - Parental Control Hotspot");
            Console.WriteLine("================================================");
            Console.WriteLine();
            
            bool running = true;
            while (running)
            {
                ShowMainMenu();
                var choice = Console.ReadLine();
                
                switch (choice?.ToLower())
                {
                    case "1":
                        await SetupHotspot();
                        break;
                    case "2":
                        await ToggleHotspot();
                        break;
                    case "3":
                        await ShowConnectedDevices();
                        break;
                    case "4":
                        await ManageContentFilters();
                        break;
                    case "5":
                        await ToggleTrafficMonitoring();
                        break;
                    case "6":
                        await ShowStatistics();
                        break;
                    case "7":
                        await ShowConfiguration();
                        break;
                    case "8":
                    case "q":
                    case "quit":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
                
                if (running)
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
            
            await Cleanup();
        }

        private void ShowMainMenu()
        {
            Console.WriteLine("Main Menu:");
            Console.WriteLine("1. Setup Hotspot");
            Console.WriteLine("2. Toggle Hotspot (Start/Stop)");
            Console.WriteLine("3. View Connected Devices");
            Console.WriteLine("4. Manage Content Filters");
            Console.WriteLine("5. Toggle Traffic Monitoring");
            Console.WriteLine("6. View Statistics");
            Console.WriteLine("7. Show Configuration");
            Console.WriteLine("8. Quit");
            Console.WriteLine();
            Console.Write("Select an option (1-8): ");
        }

        private async Task SetupHotspot()
        {
            Console.Clear();
            Console.WriteLine("🔧 Hotspot Setup");
            Console.WriteLine("================");
            
            Console.Write("Enter Hotspot Name (SSID): ");
            var ssid = Console.ReadLine();
            
            Console.Write("Enter Hotspot Password (min 8 chars): ");
            var password = ReadPassword();
            
            if (string.IsNullOrWhiteSpace(ssid) || string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                Console.WriteLine("❌ Invalid SSID or password. Password must be at least 8 characters.");
                return;
            }
            
            Console.WriteLine("\n⏳ Setting up hotspot...");
            var success = await _hotspotService.EnableHotspotAsync(ssid, password);
            
            if (success)
            {
                Console.WriteLine("✅ Hotspot setup successful!");
                Console.WriteLine($"   SSID: {ssid}");
                Console.WriteLine("   Devices can now connect to your hotspot.");
                
                _hotspotService.StartDeviceMonitoring();
            }
            else
            {
                Console.WriteLine("❌ Failed to setup hotspot. Make sure you're running as Administrator.");
            }
        }

        private async Task ToggleHotspot()
        {
            Console.Clear();
            Console.WriteLine("🔄 Toggle Hotspot");
            Console.WriteLine("=================");
            
            if (_hotspotService.IsHotspotEnabled)
            {
                Console.WriteLine("⏳ Stopping hotspot...");
                var success = await _hotspotService.DisableHotspotAsync();
                Console.WriteLine(success ? "✅ Hotspot stopped." : "❌ Failed to stop hotspot.");
            }
            else
            {
                Console.WriteLine("Hotspot is not currently enabled. Please use 'Setup Hotspot' first.");
            }
        }

        private async Task ShowConnectedDevices()
        {
            Console.Clear();
            Console.WriteLine("📱 Connected Devices");
            Console.WriteLine("===================");
            
            if (!_hotspotService.IsHotspotEnabled)
            {
                Console.WriteLine("❌ Hotspot is not enabled.");
                return;
            }
            
            Console.WriteLine("⏳ Scanning for connected devices...");
            var devices = await _hotspotService.GetConnectedDevicesAsync();
            
            if (devices.Any())
            {
                Console.WriteLine($"\nFound {devices.Count} device(s):");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"{"Device Name",-20} {"IP Address",-15} {"MAC Address",-18} {"Status",-10} {"Category",-12}");
                Console.WriteLine(new string('-', 80));
                
                foreach (var device in devices)
                {
                    var status = device.IsBlocked ? "BLOCKED" : "ALLOWED";
                    Console.WriteLine($"{device.DeviceName,-20} {device.IpAddress,-15} {device.MacAddress,-18} {status,-10} {device.Category,-12}");
                }
                
                Console.WriteLine(new string('-', 80));
                Console.WriteLine("\nDevice Management:");
                Console.WriteLine("b) Block a device");
                Console.WriteLine("u) Unblock a device");
                Console.WriteLine("m) Mark as child device");
                Console.Write("\nSelect action (or press Enter to return): ");
                
                var action = Console.ReadLine()?.ToLower();
                if (!string.IsNullOrEmpty(action))
                {
                    await HandleDeviceAction(action, devices);
                }
            }
            else
            {
                Console.WriteLine("No devices currently connected.");
            }
        }

        private async Task HandleDeviceAction(string action, List<ConnectedDevice> devices)
        {
            Console.Write("Enter device MAC address: ");
            var macAddress = Console.ReadLine();
            
            var device = devices.FirstOrDefault(d => 
                d.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
            
            if (device == null)
            {
                Console.WriteLine("❌ Device not found.");
                return;
            }
            
            switch (action)
            {
                case "b":
                    device.IsBlocked = true;
                    Console.WriteLine($"✅ Device {device.DeviceName} has been blocked.");
                    break;
                case "u":
                    device.IsBlocked = false;
                    Console.WriteLine($"✅ Device {device.DeviceName} has been unblocked.");
                    break;
                case "m":
                    device.IsChildDevice = true;
                    Console.WriteLine($"✅ Device {device.DeviceName} marked as child device.");
                    break;
            }
        }

        private async Task ManageContentFilters()
        {
            Console.Clear();
            Console.WriteLine("🛡️  Content Filter Management");
            Console.WriteLine("==============================");
            
            while (true)
            {
                Console.WriteLine("\nFilter Options:");
                Console.WriteLine("1. View current filter rules");
                Console.WriteLine("2. Add new filter rule");
                Console.WriteLine("3. Remove filter rule");
                Console.WriteLine("4. Add blocked domain");
                Console.WriteLine("5. Remove blocked domain");
                Console.WriteLine("6. View blocked domains");
                Console.WriteLine("7. Return to main menu");
                
                Console.Write("\nSelect option (1-7): ");
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        ShowFilterRules();
                        break;
                    case "2":
                        AddFilterRule();
                        break;
                    case "3":
                        RemoveFilterRule();
                        break;
                    case "4":
                        AddBlockedDomain();
                        break;
                    case "5":
                        RemoveBlockedDomain();
                        break;
                    case "6":
                        ShowBlockedDomains();
                        break;
                    case "7":
                        return;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }

        private void ShowFilterRules()
        {
            var rules = _contentFilterService.GetAllFilterRules();
            Console.WriteLine("\nCurrent Filter Rules:");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"ID",-3} {"Name",-20} {"Type",-10} {"Pattern",-20} {"Action",-10} {"Enabled",-8}");
            Console.WriteLine(new string('-', 80));
            
            foreach (var rule in rules)
            {
                var enabled = rule.IsEnabled ? "Yes" : "No";
                Console.WriteLine($"{rule.Id,-3} {rule.Name,-20} {rule.Type,-10} {rule.Pattern,-20} {rule.Action,-10} {enabled,-8}");
            }
        }

        private void AddFilterRule()
        {
            Console.WriteLine("\nAdd New Filter Rule:");
            Console.Write("Rule Name: ");
            var name = Console.ReadLine();
            
            Console.Write("Description: ");
            var description = Console.ReadLine();
            
            Console.WriteLine("Filter Type (1=Domain, 2=URL, 3=Keyword, 4=Category): ");
            var typeInput = Console.ReadLine();
            
            if (!int.TryParse(typeInput, out int typeNum) || typeNum < 1 || typeNum > 4)
            {
                Console.WriteLine("❌ Invalid filter type.");
                return;
            }
            
            var filterType = (FilterType)(typeNum - 1);
            
            Console.Write("Pattern to match: ");
            var pattern = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pattern))
            {
                Console.WriteLine("❌ Name and pattern are required.");
                return;
            }
            
            var rule = new FilterRule
            {
                Name = name,
                Description = description ?? "",
                Type = filterType,
                Pattern = pattern,
                Action = FilterAction.Block,
                IsEnabled = true,
                Priority = 3
            };
            
            _contentFilterService.AddFilterRule(rule);
            Console.WriteLine("✅ Filter rule added successfully.");
        }

        private void RemoveFilterRule()
        {
            ShowFilterRules();
            Console.Write("\nEnter Rule ID to remove: ");
            if (int.TryParse(Console.ReadLine(), out int ruleId))
            {
                _contentFilterService.RemoveFilterRule(ruleId);
                Console.WriteLine("✅ Filter rule removed successfully.");
            }
            else
            {
                Console.WriteLine("❌ Invalid Rule ID.");
            }
        }

        private void AddBlockedDomain()
        {
            Console.Write("\nEnter domain to block: ");
            var domain = Console.ReadLine();
            
            if (!string.IsNullOrWhiteSpace(domain))
            {
                _contentFilterService.AddBlockedDomain(domain);
                Console.WriteLine($"✅ Domain '{domain}' added to block list.");
            }
            else
            {
                Console.WriteLine("❌ Invalid domain.");
            }
        }

        private void RemoveBlockedDomain()
        {
            ShowBlockedDomains();
            Console.Write("\nEnter domain to unblock: ");
            var domain = Console.ReadLine();
            
            if (!string.IsNullOrWhiteSpace(domain))
            {
                _contentFilterService.RemoveBlockedDomain(domain);
                Console.WriteLine($"✅ Domain '{domain}' removed from block list.");
            }
        }

        private void ShowBlockedDomains()
        {
            var domains = _contentFilterService.GetBlockedDomains();
            Console.WriteLine("\nBlocked Domains:");
            Console.WriteLine(new string('-', 40));
            
            if (domains.Any())
            {
                foreach (var domain in domains)
                {
                    Console.WriteLine($"• {domain}");
                }
            }
            else
            {
                Console.WriteLine("No domains currently blocked.");
            }
        }

        private async Task ToggleTrafficMonitoring()
        {
            Console.Clear();
            Console.WriteLine("📊 Traffic Monitoring");
            Console.WriteLine("====================");
            
            if (_networkTrafficService.IsMonitoring)
            {
                Console.WriteLine("⏳ Stopping traffic monitoring...");
                await _networkTrafficService.StopTrafficMonitoringAsync();
            }
            else
            {
                Console.WriteLine("⏳ Starting traffic monitoring...");
                var success = await _networkTrafficService.StartTrafficMonitoringAsync();
                
                if (success)
                {
                    Console.WriteLine("✅ Traffic monitoring started successfully.");
                    Console.WriteLine("   All web traffic will now be filtered through PocketFence.");
                }
                else
                {
                    Console.WriteLine("❌ Failed to start traffic monitoring.");
                }
            }
        }

        private async Task ShowStatistics()
        {
            Console.Clear();
            Console.WriteLine("📈 PocketFence Statistics");
            Console.WriteLine("=========================");
            
            var devices = await _hotspotService.GetConnectedDevicesAsync();
            var rules = _contentFilterService.GetAllFilterRules();
            var blockedDomains = _contentFilterService.GetBlockedDomains();
            
            Console.WriteLine($"Hotspot Status: {(_hotspotService.IsHotspotEnabled ? "✅ Enabled" : "❌ Disabled")}");
            Console.WriteLine($"Traffic Monitoring: {(_networkTrafficService.IsMonitoring ? "✅ Active" : "❌ Inactive")}");
            Console.WriteLine($"Connected Devices: {devices.Count}");
            Console.WriteLine($"  • Child Devices: {devices.Count(d => d.IsChildDevice)}");
            Console.WriteLine($"  • Blocked Devices: {devices.Count(d => d.IsBlocked)}");
            Console.WriteLine($"Filter Rules: {rules.Count}");
            Console.WriteLine($"  • Active Rules: {rules.Count(r => r.IsEnabled)}");
            Console.WriteLine($"Blocked Domains: {blockedDomains.Count}");
            
            // Try to read blocked sites log
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_sites.log");
            if (File.Exists(logPath))
            {
                var logLines = await File.ReadAllLinesAsync(logPath);
                Console.WriteLine($"Total Blocked Attempts: {logLines.Length}");
                
                if (logLines.Length > 0)
                {
                    Console.WriteLine($"Last Blocked: {logLines.LastOrDefault()}");
                }
            }
        }

        private async Task ShowConfiguration()
        {
            Console.Clear();
            Console.WriteLine("⚙️  PocketFence Configuration");
            Console.WriteLine("=============================");
            
            Console.WriteLine("System Information:");
            Console.WriteLine($"  • Application Path: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"  • OS Version: {Environment.OSVersion}");
            Console.WriteLine($"  • .NET Version: {Environment.Version}");
            Console.WriteLine($"  • Machine Name: {Environment.MachineName}");
            
            Console.WriteLine("\nNetwork Configuration:");
            Console.WriteLine($"  • Hotspot Enabled: {_hotspotService.IsHotspotEnabled}");
            Console.WriteLine($"  • Traffic Monitoring: {_networkTrafficService.IsMonitoring}");
            
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filter_config.json");
            Console.WriteLine($"  • Config File: {(File.Exists(configPath) ? "✅ Found" : "❌ Not Found")}");
            
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_sites.log");
            Console.WriteLine($"  • Log File: {(File.Exists(logPath) ? "✅ Found" : "❌ Not Found")}");
            
            Console.WriteLine("\nSecurity Notice:");
            Console.WriteLine("  ⚠️  This application requires Administrator privileges");
            Console.WriteLine("  ⚠️  Network traffic interception may trigger antivirus alerts");
            Console.WriteLine("  ⚠️  Ensure Windows Firewall allows this application");
        }

        private string ReadPassword()
        {
            var password = "";
            ConsoleKey key;
            
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                
                if (key == ConsoleKey.Backspace && password.Length > 0)
                {
                    Console.Write("\b \b");
                    password = password[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    password += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            
            Console.WriteLine();
            return password;
        }

        private async Task Cleanup()
        {
            Console.WriteLine("\n🧹 Cleaning up...");
            
            if (_hotspotService.IsHotspotEnabled)
            {
                await _hotspotService.DisableHotspotAsync();
            }
            
            if (_networkTrafficService.IsMonitoring)
            {
                await _networkTrafficService.StopTrafficMonitoringAsync();
            }
            
            Console.WriteLine("✅ Cleanup completed. Goodbye!");
        }
    }
}
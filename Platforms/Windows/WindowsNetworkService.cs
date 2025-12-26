using PocketFence_Simple.Interfaces;
using PocketFence_Simple.Models;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PocketFence_Simple.Platforms.Windows
{
    public class WindowsNetworkService : INetworkService
    {
        private bool _isHotspotEnabled = false;
        private bool _isMonitoring = false;

        public bool IsHotspotEnabled => _isHotspotEnabled;
        public bool IsMonitoring => _isMonitoring;

        public event EventHandler<string>? HotspotStatusChanged;
#pragma warning disable CS0067 // Event is never used - part of interface contract
        public event EventHandler<ConnectedDevice>? DeviceConnected;
        public event EventHandler<ConnectedDevice>? DeviceDisconnected;
#pragma warning restore CS0067
        public event EventHandler<string>? TrafficMonitoringStatusChanged;

        public async Task<bool> EnableHotspotAsync(string ssid, string password)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan set hostednetwork mode=allow ssid=\"{ssid}\" key=\"{password}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        await StartHostedNetworkAsync();
                        _isHotspotEnabled = true;
                        HotspotStatusChanged?.Invoke(this, "Hotspot enabled successfully");
                        return true;
                    }
                }
                
                HotspotStatusChanged?.Invoke(this, "Failed to enable hotspot");
                return false;
            }
            catch (Exception ex)
            {
                HotspotStatusChanged?.Invoke(this, $"Error enabling hotspot: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisableHotspotAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan stop hostednetwork",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        _isHotspotEnabled = false;
                        HotspotStatusChanged?.Invoke(this, "Hotspot disabled successfully");
                        return true;
                    }
                }
                
                HotspotStatusChanged?.Invoke(this, "Failed to disable hotspot");
                return false;
            }
            catch (Exception ex)
            {
                HotspotStatusChanged?.Invoke(this, $"Error disabling hotspot: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ConnectedDevice>> GetConnectedDevicesAsync()
        {
            var devices = new List<ConnectedDevice>();
            
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    devices = ParseArpOutput(output);
                }
            }
            catch (Exception ex)
            {
                // Log error but return empty list
                System.Diagnostics.Debug.WriteLine($"Error getting connected devices: {ex.Message}");
            }
            
            return devices;
        }

        public async Task<bool> StartTrafficMonitoringAsync()
        {
            if (_isMonitoring)
                return true;

            try
            {
                _isMonitoring = true;
                TrafficMonitoringStatusChanged?.Invoke(this, "Traffic monitoring started");
                
                // Start monitoring in background
                _ = Task.Run(async () =>
                {
                    while (_isMonitoring)
                    {
                        // Monitor network traffic here
                        await Task.Delay(5000);
                    }
                });
                
                return true;
            }
            catch (Exception ex)
            {
                _isMonitoring = false;
                TrafficMonitoringStatusChanged?.Invoke(this, $"Failed to start traffic monitoring: {ex.Message}");
                return false;
            }
        }

        public async Task StopTrafficMonitoringAsync()
        {
            if (!_isMonitoring)
                return;

            try
            {
                _isMonitoring = false;
                TrafficMonitoringStatusChanged?.Invoke(this, "Traffic monitoring stopped");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                TrafficMonitoringStatusChanged?.Invoke(this, $"Error stopping traffic monitoring: {ex.Message}");
            }
        }

        private async Task StartHostedNetworkAsync()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan start hostednetwork",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }

        private List<ConnectedDevice> ParseArpOutput(string arpOutput)
        {
            var devices = new List<ConnectedDevice>();
            
            if (string.IsNullOrWhiteSpace(arpOutput))
                return devices;
                
            var lines = arpOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || !trimmedLine.Contains("dynamic", StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                var parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var ipAddress = parts[0];
                    var macAddress = parts[1];
                    
                    var device = new ConnectedDevice
                    {
                        IpAddress = ipAddress,
                        MacAddress = macAddress,
                        FirstSeen = DateTime.Now,
                        LastSeen = DateTime.Now,
                        DeviceName = $"Device_{macAddress.Replace("-", "").AsSpan(0, Math.Min(6, macAddress.Length))}",
                        Category = DeviceCategory.Unknown
                    };
                    
                    devices.Add(device);
                }
            }
            
            return devices;
        }

        public async Task<bool> StartHotspotAsync(string ssid, string password)
        {
            return await EnableHotspotAsync(ssid, password);
        }

        public async Task<bool> StopHotspotAsync()
        {
            return await DisableHotspotAsync();
        }

        public async Task<bool> IsHotspotActiveAsync()
        {
            await Task.CompletedTask;
            return _isHotspotEnabled;
        }

        public async Task<NetworkInformation> GetNetworkInformationAsync()
        {
            var networkInfo = new NetworkInformation();
            
            try
            {
                // Get local network information
                var hostEntry = await Dns.GetHostEntryAsync(Dns.GetHostName());
                var localIP = hostEntry.AddressList
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                
                networkInfo.LocalIP = localIP?.ToString() ?? "Not Available";
                
                // Get network adapter information
                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(a => a.OperationalStatus == OperationalStatus.Up &&
                               a.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToArray();
                
                if (adapters.Length > 0)
                {
                    var adapter = adapters[0];
                    var properties = adapter.GetIPProperties();
                    
                    networkInfo.Gateway = properties.GatewayAddresses.FirstOrDefault()?.Address.ToString();
                    networkInfo.DNSServers = properties.DnsAddresses
                        .Select(dns => dns.ToString())
                        .ToArray();
                    networkInfo.AdapterName = adapter.Name;
                    networkInfo.IsConnected = adapter.OperationalStatus == OperationalStatus.Up;
                    
                    var stats = adapter.GetIPv4Statistics();
                    networkInfo.BytesSent = stats.BytesSent;
                    networkInfo.BytesReceived = stats.BytesReceived;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting network information: {ex.Message}");
            }
            
            return networkInfo;
        }
    }
}
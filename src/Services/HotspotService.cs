#if WINDOWS
using System.Management;
#endif
using System.Net;
using System.Net.NetworkInformation;
using PocketFence_Simple.Models;

namespace PocketFence_Simple.Services
{
    public class HotspotService
    {
        private bool _isHotspotEnabled = false;
        private readonly Dictionary<string, ConnectedDevice> _deviceCache = new(); // O(1) device lookup by MAC
        
        public event EventHandler<string>? HotspotStatusChanged;
        public event EventHandler<ConnectedDevice>? DeviceConnected;
        public event EventHandler<ConnectedDevice>? DeviceDisconnected;
        
        public bool IsActive => _isHotspotEnabled;

        public async Task<bool> EnableHotspotAsync(string ssid, string password)
        {
            try
            {
                // Create the hotspot profile
                var profileXml = CreateHotspotProfile(ssid, password);
                
                // Use netsh command to set up hosted network
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan set hostednetwork mode=allow ssid=\"{ssid}\" key=\"{password}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        // Start the hosted network
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
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan stop hostednetwork",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
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
                // Get ARP table entries for connected devices
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    devices = ParseArpOutput(output);
                    
                    // Update cache for O(1) lookups - merge with existing device info
                    foreach (var device in devices)
                    {
                        if (_deviceCache.TryGetValue(device.MacAddress, out var existingDevice))
                        {
                            // Preserve existing device properties and update LastSeen
                            device.FirstSeen = existingDevice.FirstSeen;
                            device.DeviceName = existingDevice.DeviceName;
                            device.IsBlocked = existingDevice.IsBlocked;
                            device.IsChildDevice = existingDevice.IsChildDevice;
                            device.IsFiltered = existingDevice.IsFiltered;
                            device.BlockedSites = existingDevice.BlockedSites;
                            device.Category = existingDevice.Category;
                        }
                        device.LastSeen = DateTime.Now;
                        _deviceCache[device.MacAddress] = device;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error instead of console output
                System.Diagnostics.Debug.WriteLine($"Error getting connected devices: {ex.Message}");
            }
            
            return devices;
        }

        private async Task<bool> StartHostedNetworkAsync()
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan start hostednetwork",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            
            return false;
        }

        private string CreateHotspotProfile(string ssid, string password)
        {
            return $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{ssid}</name>
    <SSIDConfig>
        <SSID>
            <name>{ssid}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPA2PSK</authentication>
                <encryption>AES</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
            <sharedKey>
                <keyType>passPhrase</keyType>
                <protected>false</protected>
                <keyMaterial>{password}</keyMaterial>
            </sharedKey>
        </security>
    </MSM>
</WLANProfile>";
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

        /// <summary>
        /// Get device by MAC address with O(1) lookup performance
        /// </summary>
        /// <param name="macAddress">MAC address of the device</param>
        /// <returns>ConnectedDevice if found, null otherwise</returns>
        public ConnectedDevice? GetDeviceByMacAddress(string macAddress)
        {
            return _deviceCache.TryGetValue(macAddress, out var device) ? device : null;
        }

        /// <summary>
        /// Check if device is connected with O(1) lookup performance
        /// </summary>
        /// <param name="macAddress">MAC address of the device</param>
        /// <returns>True if device is connected, false otherwise</returns>
        public bool IsDeviceConnected(string macAddress)
        {
            return _deviceCache.ContainsKey(macAddress);
        }

        public bool IsHotspotEnabled => _isHotspotEnabled;

        public void StartDeviceMonitoring()
        {
            // Start monitoring for device connections/disconnections
            Task.Run(async () =>
            {
                var previousDevices = new Dictionary<string, ConnectedDevice>();
                
                while (_isHotspotEnabled)
                {
                    try
                    {
                        var currentDevices = await GetConnectedDevicesAsync();
                        var currentDeviceMap = currentDevices.ToDictionary(d => d.MacAddress, d => d);
                        
                        // Check for new devices - O(1) lookup instead of O(n²)
                        foreach (var device in currentDevices)
                        {
                            if (!previousDevices.ContainsKey(device.MacAddress))
                            {
                                DeviceConnected?.Invoke(this, device);
                            }
                        }
                        
                        // Check for disconnected devices - O(1) lookup
                        foreach (var kvp in previousDevices)
                        {
                            if (!currentDeviceMap.ContainsKey(kvp.Key))
                            {
                                DeviceDisconnected?.Invoke(this, kvp.Value);
                            }
                        }
                        
                        previousDevices = currentDeviceMap;
                        await Task.Delay(5000); // Check every 5 seconds
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in device monitoring: {ex.Message}");
                        await Task.Delay(10000); // Wait longer on error
                    }
                }
            });
        }
    }
}
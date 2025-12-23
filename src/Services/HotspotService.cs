using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using PocketFence.Models;

namespace PocketFence.Services
{
    public class HotspotService
    {
        private ManagementObjectSearcher? _wlanSearcher;
        private bool _isHotspotEnabled = false;
        
        public event EventHandler<string>? HotspotStatusChanged;
        public event EventHandler<ConnectedDevice>? DeviceConnected;
        public event EventHandler<ConnectedDevice>? DeviceDisconnected;

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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting connected devices: {ex.Message}");
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
            var lines = arpOutput.Split('\n');
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("dynamic"))
                    continue;
                    
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var device = new ConnectedDevice
                    {
                        IpAddress = parts[0].Trim(),
                        MacAddress = parts[1].Trim(),
                        FirstSeen = DateTime.Now,
                        LastSeen = DateTime.Now,
                        DeviceName = $"Device_{parts[1].Replace("-", "").Substring(0, 6)}",
                        Category = DeviceCategory.Unknown
                    };
                    
                    devices.Add(device);
                }
            }
            
            return devices;
        }

        public bool IsHotspotEnabled => _isHotspotEnabled;

        public void StartDeviceMonitoring()
        {
            // Start monitoring for device connections/disconnections
            Task.Run(async () =>
            {
                var previousDevices = new List<ConnectedDevice>();
                
                while (_isHotspotEnabled)
                {
                    try
                    {
                        var currentDevices = await GetConnectedDevicesAsync();
                        
                        // Check for new devices
                        foreach (var device in currentDevices)
                        {
                            if (!previousDevices.Any(d => d.MacAddress == device.MacAddress))
                            {
                                DeviceConnected?.Invoke(this, device);
                            }
                        }
                        
                        // Check for disconnected devices
                        foreach (var device in previousDevices)
                        {
                            if (!currentDevices.Any(d => d.MacAddress == device.MacAddress))
                            {
                                DeviceDisconnected?.Invoke(this, device);
                            }
                        }
                        
                        previousDevices = currentDevices;
                        await Task.Delay(5000); // Check every 5 seconds
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in device monitoring: {ex.Message}");
                        await Task.Delay(10000); // Wait longer on error
                    }
                }
            });
        }
    }
}
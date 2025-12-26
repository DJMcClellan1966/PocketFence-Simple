using PocketFence_Simple.Models;

namespace PocketFence_Simple.Interfaces
{
    public interface INetworkService
    {
        Task<bool> EnableHotspotAsync(string ssid, string password);
        Task<bool> StartHotspotAsync(string ssid, string password);
        Task<bool> StopHotspotAsync();
        Task<bool> DisableHotspotAsync();
        Task<bool> IsHotspotActiveAsync();
        Task<NetworkInformation> GetNetworkInformationAsync();
        Task<List<ConnectedDevice>> GetConnectedDevicesAsync();
        Task<bool> StartTrafficMonitoringAsync();
        Task StopTrafficMonitoringAsync();
        bool IsHotspotEnabled { get; }
        bool IsMonitoring { get; }
        
        event EventHandler<string>? HotspotStatusChanged;
        event EventHandler<ConnectedDevice>? DeviceConnected;
        event EventHandler<ConnectedDevice>? DeviceDisconnected;
        event EventHandler<string>? TrafficMonitoringStatusChanged;
    }
}
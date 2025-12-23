namespace PocketFence.Models
{
    public class ConnectedDevice
    {
        public string MacAddress { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsChildDevice { get; set; }
        public long DataUsage { get; set; }
        public List<string> BlockedSites { get; set; } = new List<string>();
        public DeviceCategory Category { get; set; }
    }

    public enum DeviceCategory
    {
        Unknown,
        Smartphone,
        Tablet,
        Laptop,
        GameConsole,
        SmartTV,
        IoTDevice
    }
}
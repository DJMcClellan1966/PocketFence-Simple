namespace PocketFence_Simple.Models
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
        public bool IsFiltered { get; set; }
        public string FilterStatus => IsFiltered ? "Protected" : "Unprotected";
        public Color FilterStatusColor => IsFiltered ? Colors.Green : Colors.Orange;
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
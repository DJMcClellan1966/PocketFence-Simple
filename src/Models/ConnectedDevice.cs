using System.ComponentModel;

namespace PocketFence_Simple.Models
{
    public class ConnectedDevice : INotifyPropertyChanged
    {
        private bool _isFiltered;
        
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the device
        public string Name => DeviceName; // Alias for DeviceName for compatibility
        public string MacAddress { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty; // e.g., "Desktop", "Mobile", "Tablet", "Smart TV", etc.
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsChildDevice { get; set; }
        
        public bool IsFiltered 
        { 
            get => _isFiltered;
            set 
            { 
                _isFiltered = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilterStatus));
                OnPropertyChanged(nameof(FilterStatusColor));
            }
        }
        
        public string FilterStatus => IsFiltered ? "Protected" : "Unprotected";
        public string FilterStatusColor => IsFiltered ? "#4CAF50" : "#FF9800"; // Green or Orange
        public long DataUsage { get; set; }
        public List<string> BlockedSites { get; set; } = new List<string>();
        public DeviceCategory Category { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
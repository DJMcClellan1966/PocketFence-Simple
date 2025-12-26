using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PocketFence_Simple.Services;
using PocketFence_Simple.Interfaces;

namespace PocketFence_Simple
{
    public partial class NetworkPage : ContentPage, INotifyPropertyChanged
    {
        private readonly INetworkService _networkService;
        private bool _isHotspotEnabled = false;
        private string _hotspotSSID = "PocketFence";
        private string _hotspotPassword = "password123";
        private string _hotspotStatus = "Disabled";
        private Color _hotspotStatusColor = Colors.Red;
        private string _localIPAddress = "Not Available";
        private string _gatewayAddress = "Not Available";
        private string _dnsServers = "Not Available";
        private int _connectedDevicesCount = 0;
        private string _totalDataTransferred = "0 MB";
        private int _blockedRequests = 0;
        private string _uploadSpeed = "0 KB/s";
        private string _downloadSpeed = "0 KB/s";
        private bool _isNotBusy = true;

        public NetworkPage(INetworkService networkService)
        {
            InitializeComponent();
            _networkService = networkService;
            BindingContext = this;
            
            // Initialize commands to fix nullable warnings
            ApplyHotspotSettingsCommand = new Command(async () => await ApplyHotspotSettingsAsync());
            RefreshNetworkCommand = new Command(async () => await LoadNetworkInfoAsync());
            RestartHotspotCommand = new Command(async () => await RestartHotspotAsync());
            ViewConnectionLogCommand = new Command(async () => await ViewConnectionLogAsync());
            ExportTrafficReportCommand = new Command(async () => await ExportTrafficReportAsync());
            
            _ = LoadNetworkInfoAsync();
        }

        public bool IsHotspotEnabled
        {
            get => _isHotspotEnabled;
            set
            {
                _isHotspotEnabled = value;
                OnPropertyChanged();
                UpdateHotspotStatus();
            }
        }

        public string HotspotSSID
        {
            get => _hotspotSSID;
            set
            {
                _hotspotSSID = value;
                OnPropertyChanged();
            }
        }

        public string HotspotPassword
        {
            get => _hotspotPassword;
            set
            {
                _hotspotPassword = value;
                OnPropertyChanged();
            }
        }

        public string HotspotStatus
        {
            get => _hotspotStatus;
            set
            {
                _hotspotStatus = value;
                OnPropertyChanged();
            }
        }

        public Color HotspotStatusColor
        {
            get => _hotspotStatusColor;
            set
            {
                _hotspotStatusColor = value;
                OnPropertyChanged();
            }
        }

        public string LocalIPAddress
        {
            get => _localIPAddress;
            set
            {
                _localIPAddress = value;
                OnPropertyChanged();
            }
        }

        public string GatewayAddress
        {
            get => _gatewayAddress;
            set
            {
                _gatewayAddress = value;
                OnPropertyChanged();
            }
        }

        public string DNSServers
        {
            get => _dnsServers;
            set
            {
                _dnsServers = value;
                OnPropertyChanged();
            }
        }

        public int ConnectedDevicesCount
        {
            get => _connectedDevicesCount;
            set
            {
                _connectedDevicesCount = value;
                OnPropertyChanged();
            }
        }

        public string TotalDataTransferred
        {
            get => _totalDataTransferred;
            set
            {
                _totalDataTransferred = value;
                OnPropertyChanged();
            }
        }

        public int BlockedRequests
        {
            get => _blockedRequests;
            set
            {
                _blockedRequests = value;
                OnPropertyChanged();
            }
        }

        public string UploadSpeed
        {
            get => _uploadSpeed;
            set
            {
                _uploadSpeed = value;
                OnPropertyChanged();
            }
        }

        public string DownloadSpeed
        {
            get => _downloadSpeed;
            set
            {
                _downloadSpeed = value;
                OnPropertyChanged();
            }
        }

        public bool IsNotBusy
        {
            get => _isNotBusy;
            set
            {
                _isNotBusy = value;
                OnPropertyChanged();
            }
        }

        public ICommand ApplyHotspotSettingsCommand { get; private set; }
        public ICommand RefreshNetworkCommand { get; private set; }
        public ICommand RestartHotspotCommand { get; private set; }
        public ICommand ViewConnectionLogCommand { get; private set; }
        public ICommand ExportTrafficReportCommand { get; private set; }



        private async Task LoadNetworkInfoAsync()
        {
            IsNotBusy = false;
            try
            {
                // Load network information
                var networkInfo = await _networkService.GetNetworkInformationAsync();
                LocalIPAddress = networkInfo.LocalIP ?? "Not Available";
                GatewayAddress = networkInfo.Gateway ?? "Not Available";
                DNSServers = string.Join(", ", networkInfo.DNSServers ?? new[] { "Not Available" });
                
                // Load connected devices count
                var devices = await _networkService.GetConnectedDevicesAsync();
                ConnectedDevicesCount = devices.Count();
                
                // Check hotspot status
                IsHotspotEnabled = await _networkService.IsHotspotActiveAsync();
                
                // Update traffic statistics (placeholder values)
                TotalDataTransferred = "125 MB";
                BlockedRequests = 42;
                UploadSpeed = "2.5 KB/s";
                DownloadSpeed = "15.3 KB/s";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load network information: {ex.Message}", "OK");
            }
            finally
            {
                IsNotBusy = true;
            }
        }

        private async Task ApplyHotspotSettingsAsync()
        {
            if (string.IsNullOrWhiteSpace(HotspotSSID) || string.IsNullOrWhiteSpace(HotspotPassword))
            {
                await DisplayAlert("Error", "Please enter both SSID and password", "OK");
                return;
            }

            IsNotBusy = false;
            try
            {
                if (IsHotspotEnabled)
                {
                    var success = await _networkService.StartHotspotAsync(HotspotSSID, HotspotPassword);
                    if (success)
                    {
                        await DisplayAlert("Success", "Hotspot settings applied successfully", "OK");
                        UpdateHotspotStatus();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to apply hotspot settings", "OK");
                        IsHotspotEnabled = false;
                    }
                }
                else
                {
                    var success = await _networkService.StopHotspotAsync();
                    if (success)
                    {
                        await DisplayAlert("Success", "Hotspot stopped successfully", "OK");
                        UpdateHotspotStatus();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to stop hotspot", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to apply hotspot settings: {ex.Message}", "OK");
            }
            finally
            {
                IsNotBusy = true;
            }
        }

        private async Task RestartHotspotAsync()
        {
            IsNotBusy = false;
            try
            {
                await _networkService.StopHotspotAsync();
                await Task.Delay(2000); // Wait before restarting
                var success = await _networkService.StartHotspotAsync(HotspotSSID, HotspotPassword);
                
                if (success)
                {
                    await DisplayAlert("Success", "Hotspot restarted successfully", "OK");
                    IsHotspotEnabled = true;
                    UpdateHotspotStatus();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to restart hotspot", "OK");
                    IsHotspotEnabled = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to restart hotspot: {ex.Message}", "OK");
            }
            finally
            {
                IsNotBusy = true;
            }
        }

        private async Task ViewConnectionLogAsync()
        {
            await DisplayAlert("Info", "Connection log viewer not yet implemented", "OK");
        }

        private async Task ExportTrafficReportAsync()
        {
            await DisplayAlert("Info", "Traffic report export not yet implemented", "OK");
        }

        private void UpdateHotspotStatus()
        {
            if (IsHotspotEnabled)
            {
                HotspotStatus = "Active";
                HotspotStatusColor = Colors.Green;
            }
            else
            {
                HotspotStatus = "Disabled";
                HotspotStatusColor = Colors.Red;
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
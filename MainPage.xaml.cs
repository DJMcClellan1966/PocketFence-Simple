using PocketFence_Simple.Interfaces;
using System.Collections.ObjectModel;

namespace PocketFence_Simple;

public partial class MainPage : ContentPage
{
    private readonly INetworkService _networkService;
    private readonly ISystemUtilsService _systemUtils;
    public ObservableCollection<ActivityItem> RecentActivity { get; set; } = new();

    public MainPage(INetworkService networkService, ISystemUtilsService systemUtils)
    {
        InitializeComponent();
        _networkService = networkService;
        _systemUtils = systemUtils;
        
        RecentActivityCollectionView.ItemsSource = RecentActivity;
        
        // Subscribe to events
        _networkService.HotspotStatusChanged += OnHotspotStatusChanged;
        _networkService.DeviceConnected += OnDeviceConnected;
        _networkService.DeviceDisconnected += OnDeviceDisconnected;
        _networkService.TrafficMonitoringStatusChanged += OnTrafficMonitoringStatusChanged;
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Check system requirements
            var systemSupported = _systemUtils.CheckSystemVersion();
            var wifiSupported = await _systemUtils.CheckWifiAdapterAsync();
            
            // Update system info
            var systemInfo = _systemUtils.GetSystemInfo();
            SystemInfoLabel.Text = systemInfo;
            
            // Update UI based on capabilities
            if (!systemSupported || !wifiSupported)
            {
                AddActivity("⚠️", "System limitations detected", "Some features may not work properly", "Warning");
            }
            else
            {
                AddActivity("✅", "System check completed", "All features available", "Success");
            }
            
            // Update status displays
            UpdateStatusDisplays();
            
            // Setup application directories
            _systemUtils.SetupApplicationDirectories();
            
        }
        catch (Exception ex)
        {
            AddActivity("❌", "Initialization failed", ex.Message, "Error");
            await DisplayAlert("Error", $"Failed to initialize: {ex.Message}", "OK");
        }
    }

    private void UpdateStatusDisplays()
    {
        // Update hotspot status
        HotspotStatusLabel.Text = _networkService.IsHotspotEnabled ? "Enabled" : "Disabled";
        ToggleHotspotButton.Text = _networkService.IsHotspotEnabled ? "Disable Hotspot" : "Enable Hotspot";
        
        // Update monitor status  
        MonitorStatusLabel.Text = _networkService.IsMonitoring ? "Active" : "Stopped";
        ToggleMonitorButton.Text = _networkService.IsMonitoring ? "Stop Monitoring" : "Start Monitoring";
        
        // Update device count
        _ = UpdateDeviceCountAsync();
    }

    private async Task UpdateDeviceCountAsync()
    {
        try
        {
            var devices = await _networkService.GetConnectedDevicesAsync();
            DeviceCountLabel.Text = $"{devices.Count} device(s)";
        }
        catch (Exception ex)
        {
            DeviceCountLabel.Text = "Error getting devices";
            _systemUtils.LogEvent($"Error updating device count: {ex.Message}", "ERROR");
        }
    }

    private async void OnToggleHotspotClicked(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            button!.IsEnabled = false;
            button.Text = "Please wait...";

            if (_networkService.IsHotspotEnabled)
            {
                var success = await _networkService.DisableHotspotAsync();
                AddActivity("🔥", "Hotspot", success ? "Disabled successfully" : "Failed to disable", success ? "Success" : "Error");
            }
            else
            {
                // Show hotspot configuration dialog
                await ShowHotspotConfigDialog();
            }
        }
        catch (Exception ex)
        {
            AddActivity("❌", "Hotspot Error", ex.Message, "Error");
            await DisplayAlert("Error", $"Hotspot operation failed: {ex.Message}", "OK");
        }
        finally
        {
            UpdateStatusDisplays();
        }
    }

    private async Task ShowHotspotConfigDialog()
    {
        var ssid = await DisplayPromptAsync("Hotspot Configuration", "Enter network name (SSID):", initialValue: "PocketFence-Hotspot");
        if (string.IsNullOrWhiteSpace(ssid))
            return;

        var password = await DisplayPromptAsync("Hotspot Configuration", "Enter password (min 8 characters):", keyboard: Keyboard.Default);
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            await DisplayAlert("Error", "Password must be at least 8 characters long", "OK");
            return;
        }

        var success = await _networkService.EnableHotspotAsync(ssid, password);
        AddActivity("🔥", "Hotspot", success ? $"Enabled: {ssid}" : "Failed to enable", success ? "Success" : "Error");
    }

    private async void OnToggleMonitorClicked(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            button!.IsEnabled = false;
            button.Text = "Please wait...";

            if (_networkService.IsMonitoring)
            {
                await _networkService.StopTrafficMonitoringAsync();
            }
            else
            {
                var success = await _networkService.StartTrafficMonitoringAsync();
                if (!success)
                {
                    await DisplayAlert("Error", "Failed to start traffic monitoring. Administrator privileges may be required.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            AddActivity("❌", "Monitor Error", ex.Message, "Error");
            await DisplayAlert("Error", $"Monitoring operation failed: {ex.Message}", "OK");
        }
        finally
        {
            UpdateStatusDisplays();
        }
    }

    private async void OnViewDevicesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DevicesPage");
    }

    private async void OnConfigureFilterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//FilterPage");
    }

    private void OnHotspotStatusChanged(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddActivity("🔥", "Hotspot", message, "Info");
            UpdateStatusDisplays();
        });
    }

    private void OnDeviceConnected(object? sender, PocketFence_Simple.Models.ConnectedDevice device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddActivity("📱", "Device Connected", $"{device.DeviceName} ({device.IpAddress})", "Success");
            _ = UpdateDeviceCountAsync();
        });
    }

    private void OnDeviceDisconnected(object? sender, PocketFence_Simple.Models.ConnectedDevice device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddActivity("📱", "Device Disconnected", $"{device.DeviceName} ({device.IpAddress})", "Info");
            _ = UpdateDeviceCountAsync();
        });
    }

    private void OnTrafficMonitoringStatusChanged(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddActivity("📊", "Traffic Monitor", message, "Info");
            UpdateStatusDisplays();
        });
    }

    private void AddActivity(string icon, string title, string message, string status)
    {
        var statusColor = status switch
        {
            "Success" => Colors.Green,
            "Warning" => Colors.Orange,
            "Error" => Colors.Red,
            _ => Colors.Gray
        };

        var activity = new ActivityItem
        {
            Icon = icon,
            Message = $"{title}: {message}",
            Timestamp = DateTime.Now.ToString("HH:mm:ss"),
            Status = status,
            StatusColor = statusColor
        };

        // Add to the beginning of the collection
        RecentActivity.Insert(0, activity);

        // Keep only the last 10 items
        while (RecentActivity.Count > 10)
        {
            RecentActivity.RemoveAt(RecentActivity.Count - 1);
        }
    }
}

public class ActivityItem
{
    public string Icon { get; set; } = "";
    public string Message { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Status { get; set; } = "";
    public Color StatusColor { get; set; } = Colors.Gray;
}
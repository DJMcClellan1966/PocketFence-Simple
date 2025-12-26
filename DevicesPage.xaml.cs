using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PocketFence_Simple.Services;
using PocketFence_Simple.Models;
using PocketFence_Simple.Interfaces;

namespace PocketFence_Simple
{
    public partial class DevicesPage : ContentPage, INotifyPropertyChanged
    {
        private readonly INetworkService _networkService;
        private ObservableCollection<ConnectedDevice> _connectedDevices = new();
        private bool _isLoading = false;

        public DevicesPage(INetworkService networkService)
        {
            InitializeComponent();
            _networkService = networkService;
            BindingContext = this;
            RefreshCommand = new Command(async () => await RefreshDevicesAsync());
            StartHotspotCommand = new Command(async () => await StartHotspotAsync());
            
            _ = LoadDevicesAsync();
        }

        public ObservableCollection<ConnectedDevice> ConnectedDevices
        {
            get => _connectedDevices;
            set
            {
                _connectedDevices = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(TotalDevices));
                OnPropertyChanged(nameof(FilteredDevices));
                OnPropertyChanged(nameof(UnfilteredDevices));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsEmpty => !ConnectedDevices.Any();
        public int TotalDevices => ConnectedDevices.Count;
        public int FilteredDevices => ConnectedDevices.Count(d => d.IsFiltered);
        public int UnfilteredDevices => ConnectedDevices.Count(d => !d.IsFiltered);

        public ICommand RefreshCommand { get; }
        public ICommand StartHotspotCommand { get; }

        private async Task LoadDevicesAsync()
        {
            IsLoading = true;
            try
            {
                var devices = await _networkService.GetConnectedDevicesAsync();
                ConnectedDevices = new ObservableCollection<ConnectedDevice>(devices);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load devices: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshDevicesAsync()
        {
            await LoadDevicesAsync();
        }

        private async Task StartHotspotAsync()
        {
            IsLoading = true;
            try
            {
                var success = await _networkService.StartHotspotAsync("PocketFence", "password123");
                if (success)
                {
                    await DisplayAlert("Success", "Hotspot started successfully", "OK");
                    await LoadDevicesAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Failed to start hotspot", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to start hotspot: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
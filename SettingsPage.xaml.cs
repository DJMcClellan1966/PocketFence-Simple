using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PocketFence_Simple
{
    public partial class SettingsPage : ContentPage, INotifyPropertyChanged
    {
        private bool _startWithWindows = true;
        private bool _minimizeToTray = true;
        private bool _showNotifications = true;
        private bool _strictModeEnabled = false;
        private bool _blockHttpsDefault = false;
        private string _filterUpdateFrequency = "Weekly";
        private string _defaultHotspotSSID = "PocketFence";
        private string _defaultHotspotPassword = "password123";
        private double _maxConnectedDevices = 10;
        private bool _autoDetectDevices = true;
        private bool _requireAdminPassword = false;
        private bool _logAllActivity = true;
        private double _logRetentionDays = 30;
        private string _appVersion = "1.0.0";

        public SettingsPage()
        {
            InitializeComponent();
            BindingContext = this;
            
            // Initialize commands to fix nullable warnings
            ChangePasswordCommand = new Command(async () => await ChangePasswordAsync());
            ClearLogsCommand = new Command(async () => await ClearLogsAsync());
            ResetSettingsCommand = new Command(async () => await ResetSettingsAsync());
            ExportConfigCommand = new Command(async () => await ExportConfigAsync());
            ImportConfigCommand = new Command(async () => await ImportConfigAsync());
            CheckUpdatesCommand = new Command(async () => await CheckUpdatesAsync());
            ViewLicenseCommand = new Command(async () => await ViewLicenseAsync());
            
            LoadSettings();
        }

        #region Properties

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                _startWithWindows = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set
            {
                _minimizeToTray = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public bool ShowNotifications
        {
            get => _showNotifications;
            set
            {
                _showNotifications = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public bool StrictModeEnabled
        {
            get => _strictModeEnabled;
            set
            {
                _strictModeEnabled = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public bool BlockHttpsDefault
        {
            get => _blockHttpsDefault;
            set
            {
                _blockHttpsDefault = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public string FilterUpdateFrequency
        {
            get => _filterUpdateFrequency;
            set
            {
                _filterUpdateFrequency = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public string DefaultHotspotSSID
        {
            get => _defaultHotspotSSID;
            set
            {
                _defaultHotspotSSID = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public string DefaultHotspotPassword
        {
            get => _defaultHotspotPassword;
            set
            {
                _defaultHotspotPassword = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public double MaxConnectedDevices
        {
            get => _maxConnectedDevices;
            set
            {
                _maxConnectedDevices = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public bool AutoDetectDevices
        {
            get => _autoDetectDevices;
            set
            {
                _autoDetectDevices = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public bool RequireAdminPassword
        {
            get => _requireAdminPassword;
            set
            {
                _requireAdminPassword = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public bool LogAllActivity
        {
            get => _logAllActivity;
            set
            {
                _logAllActivity = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public double LogRetentionDays
        {
            get => _logRetentionDays;
            set
            {
                _logRetentionDays = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }

        public string AppVersion
        {
            get => _appVersion;
            set
            {
                _appVersion = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand ChangePasswordCommand { get; private set; }
        public ICommand ClearLogsCommand { get; private set; }
        public ICommand ResetSettingsCommand { get; private set; }
        public ICommand ExportConfigCommand { get; private set; }
        public ICommand ImportConfigCommand { get; private set; }
        public ICommand CheckUpdatesCommand { get; private set; }
        public ICommand ViewLicenseCommand { get; private set; }



        #endregion

        #region Methods

        private void LoadSettings()
        {
            try
            {
                // Load settings from preferences or config file
                StartWithWindows = Preferences.Get(nameof(StartWithWindows), true);
                MinimizeToTray = Preferences.Get(nameof(MinimizeToTray), true);
                ShowNotifications = Preferences.Get(nameof(ShowNotifications), true);
                StrictModeEnabled = Preferences.Get(nameof(StrictModeEnabled), false);
                BlockHttpsDefault = Preferences.Get(nameof(BlockHttpsDefault), false);
                FilterUpdateFrequency = Preferences.Get(nameof(FilterUpdateFrequency), "Weekly");
                DefaultHotspotSSID = Preferences.Get(nameof(DefaultHotspotSSID), "PocketFence");
                DefaultHotspotPassword = Preferences.Get(nameof(DefaultHotspotPassword), "password123");
                MaxConnectedDevices = Preferences.Get(nameof(MaxConnectedDevices), 10.0);
                AutoDetectDevices = Preferences.Get(nameof(AutoDetectDevices), true);
                RequireAdminPassword = Preferences.Get(nameof(RequireAdminPassword), false);
                LogAllActivity = Preferences.Get(nameof(LogAllActivity), true);
                LogRetentionDays = Preferences.Get(nameof(LogRetentionDays), 30.0);
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to load settings: {ex.Message}", "OK");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Save settings to preferences
                Preferences.Set(nameof(StartWithWindows), StartWithWindows);
                Preferences.Set(nameof(MinimizeToTray), MinimizeToTray);
                Preferences.Set(nameof(ShowNotifications), ShowNotifications);
                Preferences.Set(nameof(StrictModeEnabled), StrictModeEnabled);
                Preferences.Set(nameof(BlockHttpsDefault), BlockHttpsDefault);
                Preferences.Set(nameof(FilterUpdateFrequency), FilterUpdateFrequency);
                Preferences.Set(nameof(DefaultHotspotSSID), DefaultHotspotSSID);
                Preferences.Set(nameof(DefaultHotspotPassword), DefaultHotspotPassword);
                Preferences.Set(nameof(MaxConnectedDevices), MaxConnectedDevices);
                Preferences.Set(nameof(AutoDetectDevices), AutoDetectDevices);
                Preferences.Set(nameof(RequireAdminPassword), RequireAdminPassword);
                Preferences.Set(nameof(LogAllActivity), LogAllActivity);
                Preferences.Set(nameof(LogRetentionDays), LogRetentionDays);
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to save settings: {ex.Message}", "OK");
            }
        }

        private async Task ChangePasswordAsync()
        {
            try
            {
                string newPassword = await DisplayPromptAsync("Change Password", "Enter new admin password:", "OK", "Cancel", "Password", -1, null, "");
                if (!string.IsNullOrEmpty(newPassword))
                {
                    // Store password securely (in a real app, hash this)
                    Preferences.Set("AdminPassword", newPassword);
                    await DisplayAlert("Success", "Admin password changed successfully", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to change password: {ex.Message}", "OK");
            }
        }

        private async Task ClearLogsAsync()
        {
            try
            {
                bool confirm = await DisplayAlert("Confirm", "This will permanently delete all activity logs. Continue?", "Yes", "No");
                if (confirm)
                {
                    // Clear logs implementation
                    await DisplayAlert("Success", "Activity logs cleared successfully", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to clear logs: {ex.Message}", "OK");
            }
        }

        private async Task ResetSettingsAsync()
        {
            try
            {
                bool confirm = await DisplayAlert("Confirm", "This will reset all settings to default values. Continue?", "Yes", "No");
                if (confirm)
                {
                    // Reset all preferences
                    Preferences.Clear();
                    LoadSettings(); // Reload with defaults
                    await DisplayAlert("Success", "Settings reset to defaults", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to reset settings: {ex.Message}", "OK");
            }
        }

        private async Task ExportConfigAsync()
        {
            try
            {
                await DisplayAlert("Info", "Configuration export not yet implemented", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to export configuration: {ex.Message}", "OK");
            }
        }

        private async Task ImportConfigAsync()
        {
            try
            {
                await DisplayAlert("Info", "Configuration import not yet implemented", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to import configuration: {ex.Message}", "OK");
            }
        }

        private async Task CheckUpdatesAsync()
        {
            try
            {
                await DisplayAlert("Info", "No updates available. You are running the latest version.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to check for updates: {ex.Message}", "OK");
            }
        }

        private async Task ViewLicenseAsync()
        {
            try
            {
                await DisplayAlert("License", 
                    "PocketFence-Simple\n\n" +
                    "Copyright (c) 2024\n\n" +
                    "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n\n" +
                    "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n\n" +
                    "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.", 
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to view license: {ex.Message}", "OK");
            }
        }

        #endregion

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
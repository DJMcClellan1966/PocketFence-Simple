# PocketFence-Simple

A C# Windows desktop application that enables parents to use their device's hotspot capability to block malicious content from children's devices when connected to the hotspot.

## 🚀 Features

### 🎯 Core Functionality
- **Hotspot Management**: Create and manage Windows mobile hotspot with custom SSID and password
- **Device Discovery**: Automatically detect and list devices connecting to your hotspot
- **Content Filtering**: Block malicious websites and inappropriate content in real-time
- **Real-time Monitoring**: Monitor network traffic and view blocked attempts as they happen
- **Parental Controls**: Manage child devices and apply specific content restrictions

### 📊 Graphical Dashboard
- **Modern Windows Forms UI**: Clean, intuitive graphical interface
- **Multi-Tab Interface**: Organized sections for different features
- **Real-Time Updates**: Live statistics and device status updates
- **Device Management**: Visual device list with block/unblock controls
- **Traffic Monitoring**: Real-time log viewer with filtering capabilities

### 🛡️ Content Protection
- **Custom Filter Rules**: Create blocking rules by domain, URL, keyword, or category
- **Blocked Domains List**: Maintain a custom list of blocked websites
- **Category Filtering**: Block entire categories of content
- **Statistics Dashboard**: View comprehensive analytics on blocked content and device usage

## 🖥️ Dashboard Features

### Main Dashboard Tab
- **Hotspot Status**: Real-time status indicator and toggle controls
- **Connected Devices**: Live list of all connected devices with management options
- **Statistics Panel**: Quick overview of device counts, blocked sites, and filter rules
- **Device Actions**: Right-click context menu and buttons for device management

### Hotspot Setup Tab
- **Network Configuration**: Set custom SSID and password
- **Auto-Start Option**: Configure hotspot to start automatically
- **One-Click Apply**: Easy configuration management

### Content Filter Tab
- **Filter Rules Manager**: Add, remove, and enable/disable custom filtering rules
- **Blocked Domains**: Manage your custom blocked websites list
- **Rule Categories**: Organize rules by type (Domain, URL, Keyword, etc.)

### Traffic Monitor Tab
- **Real-Time Logging**: Live view of network traffic and blocked attempts
- **Traffic Analysis**: Monitor what devices are accessing
- **Log Management**: Clear logs and export traffic data

## 🛠️ System Requirements

- Windows 10 version 1607 (build 14393) or later
- Administrator privileges (required for network operations)
- WiFi adapter that supports hosted networks
- .NET 8.0 Windows Framework

## 📋 Installation & Setup

### Prerequisites

1. **Administrator Rights**: This application requires administrator privileges to manage network settings
2. **WiFi Adapter**: Ensure your WiFi adapter supports Windows hosted networks
3. **Windows Firewall**: You may need to allow the application through Windows Firewall

### Getting Started

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/pocketfence-simple.git
   cd pocketfence-simple
   ```

2. **Build the application**:
   ```bash
   dotnet build
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```
   
   Or run the executable as Administrator:
   ```powershell
   # Right-click on the .exe and select "Run as administrator"
   ```

## 🎯 Usage

### 1. Initial Setup

1. Launch PocketFence-Simple as Administrator
2. Select "Setup Hotspot" from the main menu
3. Enter your desired hotspot name (SSID) and password
4. The application will configure and start your hotspot

### 2. Device Management

- View connected devices in real-time
- Block/unblock specific devices
- Mark devices as "child devices" for enhanced filtering
- Monitor data usage per device

### 3. Content Filtering

- **Pre-configured Rules**: Adult content, malware, phishing sites automatically blocked
- **Custom Rules**: Add your own blocking rules by:
  - Domain (e.g., `example.com`)
  - URL patterns (e.g., `*/gaming/*`)
  - Keywords (e.g., `gambling`)
  - Categories (e.g., `violence`)

### 4. Monitoring

- Real-time traffic monitoring
- View blocked sites log
- Statistics on device usage and blocked attempts
- Export logs for analysis

## 🏗️ Project Structure

```
PocketFence-Simple/
├── src/
│   ├── Models/           # Data models (ConnectedDevice, FilterRule)
│   ├── Services/         # Core services (Hotspot, ContentFilter, NetworkTraffic)
│   ├── UI/              # User interface (Console-based)
│   └── Utils/           # Utility classes (SystemUtils)
├── .github/             # GitHub configuration
├── Program.cs           # Application entry point
├── README.md           # This file
└── PocketFence-Simple.csproj  # Project file
```

## 🔒 Security Features

- **Malware Protection**: Blocks known malicious domains
- **Phishing Detection**: Prevents access to phishing sites
- **Adult Content Filter**: Blocks inappropriate content
- **Suspicious URL Detection**: Identifies potentially harmful links
- **Real-time Threat Analysis**: Continuously monitors for new threats

## ⚙️ Configuration

The application stores configuration in:
- `filter_config.json` - Filter rules and blocked domains
- `blocked_sites.log` - Log of blocked access attempts
- `logs/application.log` - Application events and errors

## 🚨 Troubleshooting

### Common Issues

1. **"Access Denied" Error**
   - Ensure you're running as Administrator
   - Check Windows UAC settings

2. **Hotspot Won't Start**
   - Verify WiFi adapter supports hosted networks
   - Run: `netsh wlan show drivers` to check compatibility
   - Try disabling/enabling WiFi adapter

3. **Devices Can't Connect**
   - Check Windows Firewall settings
   - Verify hotspot password is correct
   - Ensure WiFi is enabled on client devices

4. **Content Filtering Not Working**
   - Ensure traffic monitoring is enabled
   - Check DNS settings on client devices
   - Verify firewall allows application traffic

## 🔧 Development

### Building from Source

```bash
# Clone repository
git clone https://github.com/your-username/pocketfence-simple.git

# Navigate to project directory
cd pocketfence-simple

# Restore packages
dotnet restore

# Build project
dotnet build

# Run tests (if available)
dotnet test

# Run application
dotnet run
```

### Dependencies

- `System.Management` - For WiFi and network management
- `System.Net.NetworkInformation` - For network monitoring
- `System.Text.Json` - For configuration serialization

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## ⚠️ Disclaimer

This application is intended for legitimate parental control and network security purposes. Users are responsible for complying with local laws and regulations regarding network monitoring and content filtering. The developers are not responsible for any misuse of this software.

## 🆘 Support

If you encounter issues or need support:

1. Check the [Troubleshooting](#-troubleshooting) section
2. Search existing [Issues](https://github.com/your-username/pocketfence-simple/issues)
3. Create a new issue with detailed information about your problem

## 🙏 Acknowledgments

- Windows Hosted Network API documentation
- .NET networking libraries
- Open-source security research communities

---

**Made with ❤️ for safer internet browsing**
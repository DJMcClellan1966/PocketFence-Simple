#nullable disable
using PocketFence.Services;
using PocketFence.Models;
using PocketFence.Utils;
using System.Windows.Forms;
using System.Drawing;

namespace PocketFence.UI
{
    public partial class MainDashboard : Form
    {
        private readonly HotspotService _hotspotService;
        private readonly ContentFilterService _contentFilterService;
        private readonly NetworkTrafficService _networkTrafficService;
        private readonly List<ConnectedDevice> _connectedDevices;
        private readonly System.Windows.Forms.Timer _refreshTimer;

        // UI Components
        private TabControl _tabControl;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripStatusLabel _deviceCountLabel;
        private ToolStripStatusLabel _timeLabel;

        // Dashboard Tab Components
        private Panel _dashboardPanel;
        private GroupBox _hotspotGroupBox;
        private GroupBox _devicesGroupBox;
        private GroupBox _statisticsGroupBox;
        private Label _hotspotStatusLabel;
        private Button _toggleHotspotButton;
        private ListView _devicesListView;
        private Label _totalDevicesLabel;
        private Label _childDevicesLabel;
        private Label _blockedDevicesLabel;
        private Label _blockedSitesLabel;

        // Hotspot Tab Components
        private Panel _hotspotPanel;
        private TextBox _ssidTextBox;
        private TextBox _passwordTextBox;
        private Button _setupHotspotButton;
        private CheckBox _autoStartCheckBox;

        // Content Filter Tab Components
        private Panel _contentFilterPanel;
        private ListView _filterRulesListView;
        private ListView _blockedDomainsListView;
        private Button _addRuleButton;
        private Button _removeRuleButton;
        private Button _addDomainButton;
        private Button _removeDomainButton;

        // Traffic Monitor Tab Components
        private Panel _trafficPanel;
        private CheckBox _monitoringEnabledCheckBox;
        private RichTextBox _trafficLogTextBox;
        private Button _clearLogsButton;

        public MainDashboard()
        {
            _hotspotService = new HotspotService();
            _contentFilterService = new ContentFilterService();
            _networkTrafficService = new NetworkTrafficService(_contentFilterService);
            _connectedDevices = new List<ConnectedDevice>();

            InitializeComponent();
            SetupEventHandlers();
            
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 5000; // Refresh every 5 seconds
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }

        private void InitializeComponent()
        {
            // Main Form Setup
            this.Text = "PocketFence-Simple Dashboard";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);
            this.Icon = SystemIcons.Shield;

            // Create Tab Control
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 10)
            };

            // Create Tabs
            CreateDashboardTab();
            CreateHotspotTab();
            CreateContentFilterTab();
            CreateTrafficMonitorTab();

            // Create Status Strip
            CreateStatusStrip();

            // Add controls to form
            this.Controls.Add(_tabControl);
            this.Controls.Add(_statusStrip);
        }

        private void CreateDashboardTab()
        {
            var dashboardTab = new TabPage("Dashboard");
            _dashboardPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // Hotspot Status Group
            _hotspotGroupBox = new GroupBox
            {
                Text = "Hotspot Status",
                Size = new Size(300, 120),
                Location = new Point(10, 10)
            };

            _hotspotStatusLabel = new Label
            {
                Text = "Status: Disabled",
                Location = new Point(10, 25),
                Size = new Size(200, 23),
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
            };

            _toggleHotspotButton = new Button
            {
                Text = "Start Hotspot",
                Location = new Point(10, 55),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _toggleHotspotButton.Click += ToggleHotspotButton_Click;

            _hotspotGroupBox.Controls.AddRange(new Control[] { _hotspotStatusLabel, _toggleHotspotButton });

            // Connected Devices Group
            _devicesGroupBox = new GroupBox
            {
                Text = "Connected Devices",
                Size = new Size(650, 300),
                Location = new Point(10, 140)
            };

            _devicesListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Location = new Point(10, 25),
                Size = new Size(630, 200)
            };
            _devicesListView.Columns.Add("Device Name", 150);
            _devicesListView.Columns.Add("IP Address", 120);
            _devicesListView.Columns.Add("MAC Address", 140);
            _devicesListView.Columns.Add("Status", 80);
            _devicesListView.Columns.Add("Category", 100);
            _devicesListView.MouseClick += DevicesListView_MouseClick;

            var deviceButtonsPanel = new Panel
            {
                Location = new Point(10, 235),
                Size = new Size(630, 40)
            };

            var blockDeviceButton = new Button
            {
                Text = "Block Device",
                Location = new Point(0, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            blockDeviceButton.Click += BlockDeviceButton_Click;

            var unblockDeviceButton = new Button
            {
                Text = "Unblock Device",
                Location = new Point(110, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            unblockDeviceButton.Click += UnblockDeviceButton_Click;

            var markChildButton = new Button
            {
                Text = "Mark as Child",
                Location = new Point(220, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(255, 140, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            markChildButton.Click += MarkChildButton_Click;

            deviceButtonsPanel.Controls.AddRange(new Control[] { blockDeviceButton, unblockDeviceButton, markChildButton });
            _devicesGroupBox.Controls.AddRange(new Control[] { _devicesListView, deviceButtonsPanel });

            // Statistics Group
            _statisticsGroupBox = new GroupBox
            {
                Text = "Statistics",
                Size = new Size(300, 200),
                Location = new Point(350, 10)
            };

            _totalDevicesLabel = new Label
            {
                Text = "Total Devices: 0",
                Location = new Point(10, 25),
                Size = new Size(200, 23)
            };

            _childDevicesLabel = new Label
            {
                Text = "Child Devices: 0",
                Location = new Point(10, 55),
                Size = new Size(200, 23)
            };

            _blockedDevicesLabel = new Label
            {
                Text = "Blocked Devices: 0",
                Location = new Point(10, 85),
                Size = new Size(200, 23)
            };

            _blockedSitesLabel = new Label
            {
                Text = "Blocked Sites: 0",
                Location = new Point(10, 115),
                Size = new Size(200, 23)
            };

            _statisticsGroupBox.Controls.AddRange(new Control[] {
                _totalDevicesLabel, _childDevicesLabel, _blockedDevicesLabel, _blockedSitesLabel
            });

            _dashboardPanel.Controls.AddRange(new Control[] {
                _hotspotGroupBox, _devicesGroupBox, _statisticsGroupBox
            });

            dashboardTab.Controls.Add(_dashboardPanel);
            _tabControl.TabPages.Add(dashboardTab);
        }

        private void CreateHotspotTab()
        {
            var hotspotTab = new TabPage("Hotspot Setup");
            _hotspotPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var setupGroupBox = new GroupBox
            {
                Text = "Hotspot Configuration",
                Size = new Size(400, 200),
                Location = new Point(20, 20)
            };

            var ssidLabel = new Label
            {
                Text = "Network Name (SSID):",
                Location = new Point(10, 30),
                Size = new Size(150, 23)
            };

            _ssidTextBox = new TextBox
            {
                Location = new Point(10, 55),
                Size = new Size(200, 23),
                PlaceholderText = "Enter hotspot name"
            };

            var passwordLabel = new Label
            {
                Text = "Password (min 8 chars):",
                Location = new Point(10, 90),
                Size = new Size(150, 23)
            };

            _passwordTextBox = new TextBox
            {
                Location = new Point(10, 115),
                Size = new Size(200, 23),
                UseSystemPasswordChar = true,
                PlaceholderText = "Enter password"
            };

            _autoStartCheckBox = new CheckBox
            {
                Text = "Auto-start hotspot on application launch",
                Location = new Point(10, 150),
                Size = new Size(300, 23)
            };

            _setupHotspotButton = new Button
            {
                Text = "Apply Configuration",
                Location = new Point(220, 115),
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _setupHotspotButton.Click += SetupHotspotButton_Click;

            setupGroupBox.Controls.AddRange(new Control[] {
                ssidLabel, _ssidTextBox, passwordLabel, _passwordTextBox, _autoStartCheckBox, _setupHotspotButton
            });

            _hotspotPanel.Controls.Add(setupGroupBox);
            hotspotTab.Controls.Add(_hotspotPanel);
            _tabControl.TabPages.Add(hotspotTab);
        }

        private void CreateContentFilterTab()
        {
            var contentFilterTab = new TabPage("Content Filter");
            _contentFilterPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // Filter Rules Section
            var rulesGroupBox = new GroupBox
            {
                Text = "Filter Rules",
                Size = new Size(460, 300),
                Location = new Point(10, 10)
            };

            _filterRulesListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Location = new Point(10, 25),
                Size = new Size(440, 200),
                CheckBoxes = true
            };
            _filterRulesListView.Columns.Add("Rule Name", 150);
            _filterRulesListView.Columns.Add("Pattern", 150);
            _filterRulesListView.Columns.Add("Type", 80);
            _filterRulesListView.Columns.Add("Enabled", 60);

            var rulesButtonPanel = new Panel
            {
                Location = new Point(10, 235),
                Size = new Size(440, 40)
            };

            _addRuleButton = new Button
            {
                Text = "Add Rule",
                Location = new Point(0, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _addRuleButton.Click += AddRuleButton_Click;

            _removeRuleButton = new Button
            {
                Text = "Remove Rule",
                Location = new Point(110, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _removeRuleButton.Click += RemoveRuleButton_Click;

            rulesButtonPanel.Controls.AddRange(new Control[] { _addRuleButton, _removeRuleButton });
            rulesGroupBox.Controls.AddRange(new Control[] { _filterRulesListView, rulesButtonPanel });

            // Blocked Domains Section
            var domainsGroupBox = new GroupBox
            {
                Text = "Blocked Domains",
                Size = new Size(460, 300),
                Location = new Point(480, 10)
            };

            _blockedDomainsListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Location = new Point(10, 25),
                Size = new Size(440, 200)
            };
            _blockedDomainsListView.Columns.Add("Domain", 300);
            _blockedDomainsListView.Columns.Add("Added Date", 140);

            var domainsButtonPanel = new Panel
            {
                Location = new Point(10, 235),
                Size = new Size(440, 40)
            };

            _addDomainButton = new Button
            {
                Text = "Add Domain",
                Location = new Point(0, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _addDomainButton.Click += AddDomainButton_Click;

            _removeDomainButton = new Button
            {
                Text = "Remove Domain",
                Location = new Point(110, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _removeDomainButton.Click += RemoveDomainButton_Click;

            domainsButtonPanel.Controls.AddRange(new Control[] { _addDomainButton, _removeDomainButton });
            domainsGroupBox.Controls.AddRange(new Control[] { _blockedDomainsListView, domainsButtonPanel });

            _contentFilterPanel.Controls.AddRange(new Control[] { rulesGroupBox, domainsGroupBox });
            contentFilterTab.Controls.Add(_contentFilterPanel);
            _tabControl.TabPages.Add(contentFilterTab);
        }

        private void CreateTrafficMonitorTab()
        {
            var trafficTab = new TabPage("Traffic Monitor");
            _trafficPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            _monitoringEnabledCheckBox = new CheckBox
            {
                Text = "Enable Traffic Monitoring",
                Location = new Point(20, 20),
                Size = new Size(200, 23),
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
            };
            _monitoringEnabledCheckBox.CheckedChanged += MonitoringEnabledCheckBox_CheckedChanged;

            var logGroupBox = new GroupBox
            {
                Text = "Traffic Log",
                Location = new Point(20, 60),
                Size = new Size(900, 400)
            };

            _trafficLogTextBox = new RichTextBox
            {
                Location = new Point(10, 25),
                Size = new Size(880, 330),
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.LightGreen
            };

            _clearLogsButton = new Button
            {
                Text = "Clear Logs",
                Location = new Point(10, 365),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _clearLogsButton.Click += ClearLogsButton_Click;

            logGroupBox.Controls.AddRange(new Control[] { _trafficLogTextBox, _clearLogsButton });
            _trafficPanel.Controls.AddRange(new Control[] { _monitoringEnabledCheckBox, logGroupBox });

            trafficTab.Controls.Add(_trafficPanel);
            _tabControl.TabPages.Add(trafficTab);
        }

        private void CreateStatusStrip()
        {
            _statusStrip = new StatusStrip();

            _statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _deviceCountLabel = new ToolStripStatusLabel
            {
                Text = "Devices: 0"
            };

            _timeLabel = new ToolStripStatusLabel
            {
                Text = DateTime.Now.ToString("HH:mm:ss")
            };

            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _deviceCountLabel, _timeLabel });
        }

        private void SetupEventHandlers()
        {
            _hotspotService.HotspotStatusChanged += (sender, message) => 
            {
                this.BeginInvoke(() => UpdateHotspotStatus());
            };
            
            _hotspotService.DeviceConnected += (sender, device) => 
            {
                this.BeginInvoke(() =>
                {
                    _connectedDevices.Add(device);
                    RefreshDevicesList();
                    _statusLabel.Text = $"Device connected: {device.DeviceName}";
                });
            };
            
            _hotspotService.DeviceDisconnected += (sender, device) => 
            {
                this.BeginInvoke(() =>
                {
                    var existingDevice = _connectedDevices.FirstOrDefault(d => d.MacAddress == device.MacAddress);
                    if (existingDevice != null)
                    {
                        _connectedDevices.Remove(existingDevice);
                        RefreshDevicesList();
                        _statusLabel.Text = $"Device disconnected: {device.DeviceName}";
                    }
                });
            };
            
            _contentFilterService.SiteBlocked += (sender, blockedSite) => 
            {
                this.BeginInvoke(() =>
                {
                    var logEntry = $"[{DateTime.Now:HH:mm:ss}] BLOCKED: {blockedSite.Url} - {blockedSite.Reason}";
                    _trafficLogTextBox.AppendText(logEntry + Environment.NewLine);
                    _trafficLogTextBox.ScrollToCaret();
                    _statusLabel.Text = $"Blocked: {blockedSite.Url}";
                });
            };
        }

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            await RefreshDashboardData();
        }

        private async Task RefreshDashboardData()
        {
            try
            {
                if (_hotspotService.IsHotspotEnabled)
                {
                    var devices = await _hotspotService.GetConnectedDevicesAsync();
                    
                    // Update connected devices list
                    _connectedDevices.Clear();
                    _connectedDevices.AddRange(devices);
                    RefreshDevicesList();
                }
                
                UpdateStatistics();
                RefreshContentFilters();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error refreshing data: {ex.Message}";
            }
        }

        private void UpdateHotspotStatus()
        {
            if (_hotspotService.IsHotspotEnabled)
            {
                _hotspotStatusLabel.Text = "Status: Enabled";
                _hotspotStatusLabel.ForeColor = Color.Green;
                _toggleHotspotButton.Text = "Stop Hotspot";
                _toggleHotspotButton.BackColor = Color.FromArgb(196, 43, 28);
            }
            else
            {
                _hotspotStatusLabel.Text = "Status: Disabled";
                _hotspotStatusLabel.ForeColor = Color.Red;
                _toggleHotspotButton.Text = "Start Hotspot";
                _toggleHotspotButton.BackColor = Color.FromArgb(0, 120, 215);
            }
        }

        private void RefreshDevicesList()
        {
            _devicesListView.Items.Clear();
            
            foreach (var device in _connectedDevices)
            {
                var item = new ListViewItem(device.DeviceName);
                item.SubItems.Add(device.IpAddress);
                item.SubItems.Add(device.MacAddress);
                item.SubItems.Add(device.IsBlocked ? "BLOCKED" : "ALLOWED");
                item.SubItems.Add(device.Category.ToString());
                item.Tag = device;
                
                if (device.IsBlocked)
                    item.BackColor = Color.LightCoral;
                else if (device.IsChildDevice)
                    item.BackColor = Color.LightYellow;
                
                _devicesListView.Items.Add(item);
            }
            
            _deviceCountLabel.Text = $"Devices: {_connectedDevices.Count}";
        }

        private void UpdateStatistics()
        {
            var totalDevices = _connectedDevices.Count;
            var childDevices = _connectedDevices.Count(d => d.IsChildDevice);
            var blockedDevices = _connectedDevices.Count(d => d.IsBlocked);
            
            _totalDevicesLabel.Text = $"Total Devices: {totalDevices}";
            _childDevicesLabel.Text = $"Child Devices: {childDevices}";
            _blockedDevicesLabel.Text = $"Blocked Devices: {blockedDevices}";
            
            // Try to count blocked sites from log
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_sites.log");
            if (File.Exists(logPath))
            {
                try
                {
                    var logLines = File.ReadAllLines(logPath);
                    _blockedSitesLabel.Text = $"Blocked Sites: {logLines.Length}";
                }
                catch
                {
                    _blockedSitesLabel.Text = "Blocked Sites: N/A";
                }
            }
            else
            {
                _blockedSitesLabel.Text = "Blocked Sites: 0";
            }
        }

        private void RefreshContentFilters()
        {
            // Refresh filter rules
            _filterRulesListView.Items.Clear();
            var rules = _contentFilterService.GetAllFilterRules();
            
            foreach (var rule in rules)
            {
                var item = new ListViewItem(rule.Name) { Checked = rule.IsEnabled };
                item.SubItems.Add(rule.Pattern);
                item.SubItems.Add(rule.Type.ToString());
                item.SubItems.Add(rule.IsEnabled ? "Yes" : "No");
                item.Tag = rule;
                _filterRulesListView.Items.Add(item);
            }
            
            // Refresh blocked domains
            _blockedDomainsListView.Items.Clear();
            var domains = _contentFilterService.GetBlockedDomains();
            
            foreach (var domain in domains)
            {
                var item = new ListViewItem(domain);
                item.SubItems.Add(DateTime.Now.ToShortDateString()); // Placeholder date
                _blockedDomainsListView.Items.Add(item);
            }
        }

        // Event Handlers
        private async void ToggleHotspotButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_hotspotService.IsHotspotEnabled)
                {
                    _statusLabel.Text = "Stopping hotspot...";
                    await _hotspotService.DisableHotspotAsync();
                }
                else
                {
                    if (string.IsNullOrEmpty(_ssidTextBox.Text) || string.IsNullOrEmpty(_passwordTextBox.Text))
                    {
                        MessageBox.Show("Please configure SSID and password in the Hotspot Setup tab first.", 
                            "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _tabControl.SelectedIndex = 1; // Switch to Hotspot tab
                        return;
                    }
                    
                    _statusLabel.Text = "Starting hotspot...";
                    await _hotspotService.EnableHotspotAsync(_ssidTextBox.Text, _passwordTextBox.Text);
                }
                
                UpdateHotspotStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling hotspot: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SetupHotspotButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_ssidTextBox.Text))
            {
                MessageBox.Show("Please enter a network name (SSID).", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_passwordTextBox.Text) || _passwordTextBox.Text.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters long.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            MessageBox.Show("Hotspot configuration saved. Use the toggle button on the Dashboard tab to start the hotspot.", 
                "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DevicesListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _devicesListView.SelectedItems.Count > 0)
            {
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Block Device", null, (s, args) => BlockSelectedDevice());
                contextMenu.Items.Add("Unblock Device", null, (s, args) => UnblockSelectedDevice());
                contextMenu.Items.Add("Mark as Child Device", null, (s, args) => MarkSelectedAsChild());
                contextMenu.Show(_devicesListView, e.Location);
            }
        }

        private void BlockDeviceButton_Click(object sender, EventArgs e) => BlockSelectedDevice();
        private void UnblockDeviceButton_Click(object sender, EventArgs e) => UnblockSelectedDevice();
        private void MarkChildButton_Click(object sender, EventArgs e) => MarkSelectedAsChild();

        private void BlockSelectedDevice()
        {
            if (_devicesListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a device to block.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var device = (ConnectedDevice)_devicesListView.SelectedItems[0].Tag!;
            device.IsBlocked = true;
            RefreshDevicesList();
            _statusLabel.Text = $"Blocked device: {device.DeviceName}";
        }

        private void UnblockSelectedDevice()
        {
            if (_devicesListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a device to unblock.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var device = (ConnectedDevice)_devicesListView.SelectedItems[0].Tag;
            device.IsBlocked = false;
            RefreshDevicesList();
            _statusLabel.Text = $"Unblocked device: {device.DeviceName}";
        }

        private void MarkSelectedAsChild()
        {
            if (_devicesListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a device to mark as child device.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var device = (ConnectedDevice)_devicesListView.SelectedItems[0].Tag;
            device.IsChildDevice = true;
            RefreshDevicesList();
            _statusLabel.Text = $"Marked as child device: {device.DeviceName}";
        }

        private void AddRuleButton_Click(object sender, EventArgs e)
        {
            // TODO: Implement add filter rule dialog
            MessageBox.Show("Add Filter Rule dialog will be implemented.", "Feature Coming Soon", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RemoveRuleButton_Click(object sender, EventArgs e)
        {
            if (_filterRulesListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a rule to remove.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var rule = (FilterRule)_filterRulesListView.SelectedItems[0].Tag;
            _contentFilterService.RemoveFilterRule(rule.Id);
            RefreshContentFilters();
            _statusLabel.Text = $"Removed filter rule: {rule.Name}";
        }

        private void AddDomainButton_Click(object sender, EventArgs e)
        {
            var domain = InputDialog.Show("Enter domain to block (e.g., example.com):", "Add Blocked Domain");
            
            if (!string.IsNullOrWhiteSpace(domain))
            {
                _contentFilterService.AddBlockedDomain(domain.Trim());
                RefreshContentFilters();
                _statusLabel.Text = $"Added blocked domain: {domain}";
            }
        }

        private void RemoveDomainButton_Click(object sender, EventArgs e)
        {
            if (_blockedDomainsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a domain to remove.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var domain = _blockedDomainsListView.SelectedItems[0].Text;
            _contentFilterService.RemoveBlockedDomain(domain);
            RefreshContentFilters();
            _statusLabel.Text = $"Removed blocked domain: {domain}";
        }

        private async void MonitoringEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (_monitoringEnabledCheckBox.Checked)
                {
                    _statusLabel.Text = "Starting traffic monitoring...";
                    var success = await _networkTrafficService.StartTrafficMonitoringAsync();
                    if (!success)
                    {
                        _monitoringEnabledCheckBox.Checked = false;
                        MessageBox.Show("Failed to start traffic monitoring.", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    _statusLabel.Text = "Stopping traffic monitoring...";
                    await _networkTrafficService.StopTrafficMonitoringAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling traffic monitoring: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _monitoringEnabledCheckBox.Checked = false;
            }
        }

        private void ClearLogsButton_Click(object sender, EventArgs e)
        {
            _trafficLogTextBox.Clear();
            _statusLabel.Text = "Traffic logs cleared";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            
            // Clean up services
            if (_hotspotService.IsHotspotEnabled)
            {
                _ = _hotspotService.DisableHotspotAsync();
            }
            
            if (_networkTrafficService.IsMonitoring)
            {
                _ = _networkTrafficService.StopTrafficMonitoringAsync();
            }
            
            base.OnFormClosing(e);
        }
    }
}

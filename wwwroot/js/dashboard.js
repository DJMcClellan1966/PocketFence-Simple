// PocketFence Dashboard JavaScript
class PocketFenceDashboard {
    constructor() {
        this.connection = null;
        this.data = {
            hotspotEnabled: false,
            deviceCount: 0,
            blockedCount: 0,
            dataUsage: 0,
            activeRules: 0,
            warningCount: 0,
            filterEnabled: true,
            recentActivity: []
        };
    }

    async init() {
        console.log('🚀 Initializing PocketFence Dashboard...');
        
        // Initialize SignalR connection for real-time updates
        await this.initializeSignalR();
        
        // Load initial data
        await this.loadDashboardData();
        
        // Update UI
        this.updateUI();
        
        // Set up periodic updates
        setInterval(() => this.loadDashboardData(), 30000); // Update every 30 seconds
        
        console.log('✅ Dashboard initialized successfully');
    }

    async initializeSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/dashboardHub")
                .build();

            this.connection.on("DeviceConnected", (device) => {
                this.handleDeviceUpdate('connected', device);
            });

            this.connection.on("DeviceDisconnected", (device) => {
                this.handleDeviceUpdate('disconnected', device);
            });

            this.connection.on("ContentBlocked", (blockInfo) => {
                this.handleContentBlocked(blockInfo);
            });

            this.connection.on("StatsUpdated", (stats) => {
                this.updateStats(stats);
            });

            await this.connection.start();
            console.log('🔄 Real-time connection established');
        } catch (err) {
            console.warn('⚠️ Real-time connection failed, using polling mode:', err);
        }
    }

    async loadDashboardData() {
        try {
            // Load data from API endpoints
            const responses = await Promise.all([
                fetch('/api/dashboard/status'),
                fetch('/api/dashboard/stats'),
                fetch('/api/dashboard/activity')
            ]);

            const [status, stats, activity] = await Promise.all(
                responses.map(r => r.json())
            );

            // Update local data
            this.data = {
                ...this.data,
                ...status,
                ...stats,
                recentActivity: activity
            };

            this.updateUI();
        } catch (error) {
            console.error('❌ Failed to load dashboard data:', error);
            // Use mock data for demonstration
            this.loadMockData();
        }
    }

    loadMockData() {
        this.data = {
            hotspotEnabled: true,
            deviceCount: 3,
            blockedCount: 47,
            dataUsage: 1.2,
            activeRules: 12,
            warningCount: 2,
            filterEnabled: true,
            recentActivity: [
                {
                    icon: '🚫',
                    message: 'Blocked access to social-media.com',
                    device: 'iPhone-12',
                    time: '2 minutes ago'
                },
                {
                    icon: '📱',
                    message: 'New device connected: Android-Phone',
                    device: 'Android-Phone',
                    time: '5 minutes ago'
                },
                {
                    icon: '🛡️',
                    message: 'Added new filter rule: gaming sites',
                    device: 'System',
                    time: '10 minutes ago'
                },
                {
                    icon: '⚠️',
                    message: 'Suspicious activity detected',
                    device: 'Laptop-01',
                    time: '15 minutes ago'
                }
            ]
        };
        this.updateUI();
    }

    updateUI() {
        // Update hotspot status
        const hotspotStatus = document.getElementById('hotspot-status');
        const hotspotToggle = document.getElementById('hotspot-toggle');
        if (hotspotStatus && hotspotToggle) {
            hotspotStatus.textContent = this.data.hotspotEnabled ? 'Enabled' : 'Disabled';
            hotspotToggle.classList.toggle('active', this.data.hotspotEnabled);
        }

        // Update device count
        const deviceCount = document.getElementById('device-count');
        if (deviceCount) {
            deviceCount.textContent = this.data.deviceCount;
        }

        // Update statistics
        this.updateElement('blocked-count', this.data.blockedCount);
        this.updateElement('data-usage', `${this.data.dataUsage.toFixed(1)} GB`);
        this.updateElement('active-rules', this.data.activeRules);
        this.updateElement('warning-count', this.data.warningCount);

        // Update filter toggle
        const filterToggle = document.getElementById('filter-toggle');
        if (filterToggle) {
            filterToggle.classList.toggle('active', this.data.filterEnabled);
        }

        // Update activity list
        this.updateActivityList();
    }

    updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }

    updateActivityList() {
        const activityList = document.getElementById('activity-list');
        if (!activityList) return;

        if (this.data.recentActivity.length === 0) {
            activityList.innerHTML = `
                <div style="text-align: center; padding: 2rem; color: var(--text-secondary);">
                    📊 No recent activity
                </div>
            `;
            return;
        }

        activityList.innerHTML = this.data.recentActivity.map(activity => `
            <div class="activity-item">
                <div class="activity-icon">${activity.icon}</div>
                <div class="activity-content">
                    <div>${activity.message}</div>
                    <div class="activity-time">${activity.device} • ${activity.time}</div>
                </div>
            </div>
        `).join('');
    }

    handleDeviceUpdate(action, device) {
        if (action === 'connected') {
            this.data.deviceCount++;
            this.addActivity('📱', `New device connected: ${device.name}`, device.name, 'just now');
        } else {
            this.data.deviceCount = Math.max(0, this.data.deviceCount - 1);
            this.addActivity('📱', `Device disconnected: ${device.name}`, device.name, 'just now');
        }
        this.updateUI();
    }

    handleContentBlocked(blockInfo) {
        this.data.blockedCount++;
        this.addActivity('🚫', `Blocked access to ${blockInfo.domain}`, blockInfo.deviceName, 'just now');
        this.updateUI();
    }

    updateStats(stats) {
        this.data = { ...this.data, ...stats };
        this.updateUI();
    }

    addActivity(icon, message, device, time) {
        this.data.recentActivity.unshift({ icon, message, device, time });
        if (this.data.recentActivity.length > 10) {
            this.data.recentActivity = this.data.recentActivity.slice(0, 10);
        }
    }

    // API interaction methods
    async toggleHotspot() {
        try {
            const response = await fetch('/api/hotspot/toggle', { method: 'POST' });
            const result = await response.json();
            this.data.hotspotEnabled = result.enabled;
            this.updateUI();
        } catch (error) {
            console.error('Failed to toggle hotspot:', error);
            // Mock toggle for demo
            this.data.hotspotEnabled = !this.data.hotspotEnabled;
            this.updateUI();
        }
    }

    async toggleFilter() {
        try {
            const response = await fetch('/api/filter/toggle', { method: 'POST' });
            const result = await response.json();
            this.data.filterEnabled = result.enabled;
            this.updateUI();
        } catch (error) {
            console.error('Failed to toggle filter:', error);
            // Mock toggle for demo
            this.data.filterEnabled = !this.data.filterEnabled;
            this.updateUI();
        }
    }
}

// Global functions for UI interaction
let dashboard;

async function initializeDashboard() {
    dashboard = new PocketFenceDashboard();
    await dashboard.init();
    
    // Set up event listeners
    document.getElementById('hotspot-toggle')?.addEventListener('click', toggleHotspot);
    document.getElementById('filter-toggle')?.addEventListener('click', toggleFilter);
}

function toggleHotspot() {
    dashboard.toggleHotspot();
}

function toggleFilter() {
    dashboard.toggleFilter();
}

function showDevices() {
    window.open('/devices.html', '_blank');
}

function configureFilters() {
    window.open('/filters.html', '_blank');
}

function viewNetworkStats() {
    window.open('/network.html', '_blank');
}

function openSettings() {
    window.open('/settings.html', '_blank');
}

// ServiceWorker for offline support
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js')
            .then(() => console.log('📱 PWA features enabled'))
            .catch(err => console.log('PWA registration failed:', err));
    });
}

// Handle mobile app install
let deferredPrompt;
window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    deferredPrompt = e;
    
    // Show install button or banner
    console.log('📱 App can be installed');
});

// Add to desktop/home screen
function installApp() {
    if (deferredPrompt) {
        deferredPrompt.prompt();
        deferredPrompt.userChoice.then((choiceResult) => {
            if (choiceResult.outcome === 'accepted') {
                console.log('📱 App installed successfully');
            }
            deferredPrompt = null;
        });
    }
}

// SignalR library (CDN fallback)
if (!window.signalR) {
    window.signalR = {
        HubConnectionBuilder: function() {
            return {
                withUrl: () => this,
                build: () => ({
                    start: () => Promise.resolve(),
                    on: () => {},
                    invoke: () => Promise.resolve()
                })
            };
        }
    };
}
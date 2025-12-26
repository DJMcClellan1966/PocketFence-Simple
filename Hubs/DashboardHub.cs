using Microsoft.AspNetCore.SignalR;

namespace PocketFence_Simple.Hubs;

public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Dashboard client connected: {Context.ConnectionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "DashboardUsers");
        
        // Send initial data to new client
        await Clients.Caller.SendAsync("Connected", new { message = "Connected to PocketFence Dashboard" });
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Dashboard client disconnected: {Context.ConnectionId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DashboardUsers");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} joined group {groupName}");
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} left group {groupName}");
    }

    // Methods to send updates to all connected dashboards
    public static async Task NotifyDeviceConnected(IHubContext<DashboardHub> hubContext, object deviceInfo)
    {
        await hubContext.Clients.Group("DashboardUsers").SendAsync("DeviceConnected", deviceInfo);
    }

    public static async Task NotifyDeviceDisconnected(IHubContext<DashboardHub> hubContext, object deviceInfo)
    {
        await hubContext.Clients.Group("DashboardUsers").SendAsync("DeviceDisconnected", deviceInfo);
    }

    public static async Task NotifyContentBlocked(IHubContext<DashboardHub> hubContext, object blockInfo)
    {
        await hubContext.Clients.Group("DashboardUsers").SendAsync("ContentBlocked", blockInfo);
    }

    public static async Task NotifyStatsUpdated(IHubContext<DashboardHub> hubContext, object stats)
    {
        await hubContext.Clients.Group("DashboardUsers").SendAsync("StatsUpdated", stats);
    }
}
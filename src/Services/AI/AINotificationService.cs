using System.Collections.Concurrent;
using System.Text.Json;
using PocketFence_Simple.Models;
using Microsoft.AspNetCore.SignalR;
using PocketFence_Simple.Hubs;

namespace PocketFence_Simple.Services.AI
{
    public class AINotificationService
    {
        private readonly ILogger<AINotificationService> _logger;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ConcurrentDictionary<string, AINotification> _notifications = new();
        private readonly Timer _cleanupTimer;

        public AINotificationService(ILogger<AINotificationService> logger, IHubContext<DashboardHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;

            // Clean up old notifications every hour
            _cleanupTimer = new Timer(CleanupOldNotifications, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public async Task CreateThreatNotificationAsync(AIThreatAlert threat)
        {
            var notification = CreateNotification(
                NotificationType.ThreatDetected,
                GetThreatTitle(threat.ThreatLevel),
                $"AI blocked {threat.ThreatLevel.ToString().ToLower()}-risk content on {threat.DeviceName}",
                GetThreatPriority(threat.ThreatLevel));

            await SendNotificationAsync(notification);
        }

        public async Task CreateActionNotificationAsync(string action, string context, Dictionary<string, object>? data = null)
        {
            var notification = CreateNotification(
                NotificationType.ActionTaken,
                "AI Action",
                $"Automatically {action} in {context}",
                NotificationPriority.Normal);

            await SendNotificationAsync(notification);
        }

        public async Task CreateUpdateNotificationAsync(AIUpdateInfo updateInfo)
        {
            var notification = CreateNotification(
                NotificationType.SystemUpdate,
                updateInfo.IsSecurityUpdate ? "Security Update" : "Update Available",
                $"Version {updateInfo.Version} - {updateInfo.Description}",
                updateInfo.IsSecurityUpdate ? NotificationPriority.High : NotificationPriority.Normal);

            await SendNotificationAsync(notification);
        }

        public List<AINotification> GetActiveNotifications()
        {
            return _notifications.Values
                .Where(n => n.Timestamp > DateTime.UtcNow.AddHours(-24))
                .OrderByDescending(n => n.Timestamp)
                .ToList();
        }

        public async Task DismissNotificationAsync(string notificationId)
        {
            if (_notifications.TryRemove(notificationId, out var notification))
            {
                await _hubContext.Clients.All.SendAsync("NotificationDismissed", notificationId);
                _logger.LogDebug("Notification dismissed: {Id}", notificationId);
            }
        }

        private AINotification CreateNotification(NotificationType type, string title, string message, NotificationPriority priority)
        {
            return new AINotification
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Type = type,
                Title = title,
                Message = message,
                Priority = priority
            };
        }

        private async Task SendNotificationAsync(AINotification notification)
        {
            _notifications[notification.Id] = notification;
            await _hubContext.Clients.All.SendAsync("NewNotification", notification);
            _logger.LogInformation("Sent {Priority} notification: {Title}", notification.Priority, notification.Title);
        }

        private string GetThreatTitle(ThreatLevel level) => level switch
        {
            ThreatLevel.Critical => "🚨 Critical Threat",
            ThreatLevel.High => "⚠️ High Risk Content",
            ThreatLevel.Medium => "🔍 Suspicious Activity",
            _ => "ℹ️ Content Filtered"
        };

        private NotificationPriority GetThreatPriority(ThreatLevel level) => level switch
        {
            ThreatLevel.Critical => NotificationPriority.Critical,
            ThreatLevel.High => NotificationPriority.High,
            _ => NotificationPriority.Normal
        };

        private void CleanupOldNotifications(object? state)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var oldNotifications = _notifications.Where(n => n.Value.Timestamp < cutoff).Select(n => n.Key).ToList();
            
            foreach (var id in oldNotifications)
            {
                _notifications.TryRemove(id, out _);
            }

            if (oldNotifications.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old notifications", oldNotifications.Count);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
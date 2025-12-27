
using System.Collections.Concurrent;
using System.Text.Json;
using PocketFence_Simple.Models;
using Microsoft.AspNetCore.SignalR;
using PocketFence_Simple.Hubs;

namespace PocketFence_Simple.Services.AI;

public sealed class AINotificationService(
    ILogger<AINotificationService> logger,
    IHubContext<DashboardHub> hubContext) : IDisposable
{
    private readonly ConcurrentDictionary<string, AINotification> _notifications = [];
    private readonly PeriodicTimer _cleanupTimer = new(TimeSpan.FromHours(1));
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    // Start background cleanup task in constructor body
    static AINotificationService()
    {
        // Any static initialization if needed
    }



        public async ValueTask CreateThreatNotificationAsync(AIThreatAlert threat, CancellationToken cancellationToken = default)
        {
            var notification = CreateNotification(
                NotificationType.ThreatDetected,
                GetThreatTitle(threat.ThreatLevel),
                $"AI blocked {threat.ThreatLevel.ToString().ToLowerInvariant()}-risk content on {threat.DeviceName}",
                GetThreatPriority(threat.ThreatLevel));

            await SendNotificationAsync(notification, cancellationToken);
        }

        public async ValueTask CreateActionNotificationAsync(string action, string context, 
            Dictionary<string, object>? data = null, CancellationToken cancellationToken = default)
        {
            var notification = CreateNotification(
                NotificationType.ActionTaken,
                "AI Action",
                $"Automatically {action} in {context}",
                NotificationPriority.Normal);

            await SendNotificationAsync(notification, cancellationToken);
        }

        public async ValueTask CreateUpdateNotificationAsync(AIUpdateInfo updateInfo, CancellationToken cancellationToken = default)
        {
            var notification = CreateNotification(
                NotificationType.SystemUpdate,
                updateInfo.IsSecurityUpdate ? "Security Update" : "Update Available",
                $"Version {updateInfo.Version} - {updateInfo.Description}",
                updateInfo.IsSecurityUpdate ? NotificationPriority.High : NotificationPriority.Normal);

            await SendNotificationAsync(notification, cancellationToken);
        }

        public IReadOnlyList<AINotification> GetActiveNotifications()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            return [.. _notifications.Values
                .Where(n => n.Timestamp > cutoffTime)
                .OrderByDescending(n => n.Timestamp)];
        }

        public async ValueTask<bool> DismissNotificationAsync(string notificationId, CancellationToken cancellationToken = default)
        {
            if (_notifications.TryRemove(notificationId, out var notification))
            {
                await hubContext.Clients.All.SendAsync("NotificationDismissed", notificationId, cancellationToken);
                logger.LogDebug("Notification dismissed: {Id}", notificationId);
                return true;
            }
            return false;
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

        private async ValueTask SendNotificationAsync(AINotification notification, CancellationToken cancellationToken = default)
        {
            _notifications[notification.Id] = notification;
            await hubContext.Clients.All.SendAsync("NewNotification", notification, cancellationToken);
            logger.LogInformation("Sent {Priority} notification: {Title}", notification.Priority, notification.Title);
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

        private async Task StartCleanupAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await _cleanupTimer.WaitForNextTickAsync(cancellationToken))
                {
                    await CleanupOldNotificationsAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        private ValueTask CleanupOldNotificationsAsync()
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var oldNotificationIds = _notifications
                .Where(n => n.Value.Timestamp < cutoff)
                .Select(n => n.Key)
                .ToArray();
            
            var removedCount = 0;
            foreach (var id in oldNotificationIds)
            {
                if (_notifications.TryRemove(id, out _))
                    removedCount++;
            }

            if (removedCount > 0)
            {
                logger.LogDebug("Cleaned up {Count} old notifications", removedCount);
            }
            
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cleanupTimer.Dispose();
            _cancellationTokenSource.Dispose();
        }
}
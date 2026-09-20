namespace AIMRAN_Data_Science_Lab.Services.Notifications;

using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// Service for delivering real-time push notifications for collaboration events.
/// </summary>
public interface IRealTimeNotificationService
{
    /// <summary>
    /// Send a mention notification to a user.
    /// </summary>
    Task SendMentionNotificationAsync(string recipientUserId, string mentionedBy, string message, 
        string projectId, string resourceId, string? actionUrl = null);

    /// <summary>
    /// Send an approval request notification.
    /// </summary>
    Task SendApprovalNotificationAsync(string recipientUserId, string requestedBy, string approvalId, 
        string changeDescription, string projectId, string? actionUrl = null);

    /// <summary>
    /// Send a comment notification.
    /// </summary>
    Task SendCommentNotificationAsync(string recipientUserId, string commentedBy, string commentId, 
        string preview, string resourceId, string projectId, string? actionUrl = null);

    /// <summary>
    /// Send an edit alert notification.
    /// </summary>
    Task SendEditAlertAsync(string recipientUserId, string editorName, string resourceId, 
        string resourceName, string projectId, string? actionUrl = null);

    /// <summary>
    /// Send a conflict resolution notification.
    /// </summary>
    Task SendConflictNotificationAsync(string recipientUserId, List<string> affectedUserIds, 
        string resourceId, string conflictSummary, string projectId, string? actionUrl = null);

    /// <summary>
    /// Send approval decision notification.
    /// </summary>
    Task SendApprovalDecisionAsync(string recipientUserId, string approverName, string approvalId, 
        bool approved, string reason, string projectId, string? actionUrl = null);

    /// <summary>
    /// Send activity notification.
    /// </summary>
    Task SendActivityNotificationAsync(string recipientUserId, string userId, string activityType, 
        string description, string projectId, string? actionUrl = null);

    /// <summary>
    /// Send a batch of notifications.
    /// </summary>
    Task SendBatchNotificationsAsync(List<CollaborationNotification> notifications);

    /// <summary>
    /// Broadcast notification to multiple users.
    /// </summary>
    Task BroadcastNotificationAsync(List<string> userIds, CollaborationNotification notification);

    /// <summary>
    /// Get pending notifications for a user.
    /// </summary>
    Task<List<CollaborationNotification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);

    /// <summary>
    /// Mark notification as delivered.
    /// </summary>
    Task MarkNotificationAsDeliveredAsync(string notificationId);

    /// <summary>
    /// Mark notification as read.
    /// </summary>
    Task MarkNotificationAsReadAsync(string notificationId);

    /// <summary>
    /// Delete notification.
    /// </summary>
    Task DeleteNotificationAsync(string notificationId);

    /// <summary>
    /// Clear all notifications for a user.
    /// </summary>
    Task ClearUserNotificationsAsync(string userId);

    /// <summary>
    /// Get notification statistics for a user.
    /// </summary>
    Task<NotificationStats> GetNotificationStatsAsync(string userId);

    /// <summary>
    /// Subscribe to notification events.
    /// </summary>
    IDisposable SubscribeToNotifications(string userId, Func<CollaborationNotification, Task> handler);
}

/// <summary>
/// Statistics about a user's notifications.
/// </summary>
public class NotificationStats
{
    public string UserId { get; set; } = string.Empty;
    public int TotalNotifications { get; set; }
    public int UnreadCount { get; set; }
    public int UndeliveredCount { get; set; }
    public DateTime? OldestNotificationDate { get; set; }
    public Dictionary<string, int> NotificationsByType { get; set; } = new();
}

/// <summary>
/// In-memory implementation of real-time notification service.
/// </summary>
public class RealTimeNotificationService : IRealTimeNotificationService
{
    private readonly Dictionary<string, List<CollaborationNotification>> _userNotifications = new();
    private readonly Dictionary<string, List<Func<CollaborationNotification, Task>>> _subscriptions = new();
    private readonly object _lockObject = new();

    public async Task SendMentionNotificationAsync(string recipientUserId, string mentionedBy, string message, 
        string projectId, string resourceId, string? actionUrl = null)
    {
        var notification = new CollaborationNotification
        {
            RecipientUserId = recipientUserId,
            NotificationType = "Mention",
            TriggeredByUserId = Guid.NewGuid().ToString(),
            TriggeredByUserName = mentionedBy,
            Title = $"{mentionedBy} mentioned you",
            Message = message,
            ProjectId = projectId,
            RelatedResourceId = resourceId,
            ActionUrl = actionUrl
        };

        await AddNotificationAsync(notification);
        await BroadcastToSubscribersAsync(recipientUserId, notification);
    }

    public async Task SendApprovalNotificationAsync(string recipientUserId, string requestedBy, string approvalId, 
        string changeDescription, string projectId, string? actionUrl = null)
    {
        var notification = new CollaborationNotification
        {
            RecipientUserId = recipientUserId,
            NotificationType = "ApprovalRequest",
            TriggeredByUserName = requestedBy,
            Title = $"{requestedBy} requested your approval",
            Message = $"Please review: {changeDescription}",
            ProjectId = projectId,
            RelatedResourceId = approvalId,
            RelatedResourceType = "ApprovalRequest",
            ActionUrl = actionUrl
        };

        await AddNotificationAsync(notification);
        await BroadcastToSubscribersAsync(recipientUserId, notification);
    }

    public async Task SendCommentNotificationAsync(string recipientUserId, string commentedBy, string commentId, 
        string preview, string resourceId, string projectId, string? actionUrl = null)
    {
        var notification = new CollaborationNotification
        {
            RecipientUserId = recipientUserId,
            NotificationType = "CommentReply",
            TriggeredByUserName = commentedBy,
            Title = $"{commentedBy} commented",
            Message = $"{preview}...",
            ProjectId = projectId,
            RelatedResourceId = resourceId,
            RelatedResourceType = "Comment",
            ActionUrl = actionUrl
        };

        await AddNotificationAsync(notification);
        await BroadcastToSubscribersAsync(recipientUserId, notification);
    }

    public async Task SendEditAlertAsync(string recipientUserId, string editorName, string resourceId, 
        string resourceName, string projectId, string? actionUrl = null)
    {
        var notification = new CollaborationNotification
        {
            RecipientUserId = recipientUserId,
            NotificationType = "ResourceUpdate",
            TriggeredByUserName = editorName,
            Title = $"{editorName} edited {resourceName}",
            Message = "Resource has been updated. View changes?",
            ProjectId = projectId,
            RelatedResourceId = resourceId,
            ActionUrl = actionUrl
        };

        await AddNotificationAsync(notification);
        await BroadcastToSubscribersAsync(recipientUserId, notification);
    }

    public async Task SendConflictNotificationAsync(string recipientUserId, List<string> affectedUserIds, 
        string resourceId, string conflictSummary, string projectId, string? actionUrl = null)
    {
        var notification = new CollaborationNotification
        {
            RecipientUserId = recipientUserId,
            NotificationType = "Alert",
            Title = "Edit conflict detected",
            Message = conflictSummary,
            ProjectId = projectId,
            RelatedResourceId = resourceId,
            RelatedResourceType = "Conflict",
            ActionUrl = actionUrl
        };

        notification.Metadata["affectedUsers"] = affectedUserIds;

        await AddNotificationAsync(notification);
        await BroadcastToSubscribersAsync(recipientUserId, notification);
    }

    public async Task SendApprovalDecisionAsync(string recipientUserId, string approverName, string approvalId, 
        bool approved, string reason, string projectId, string? actionUrl = null)
    {
        var notification = new CollaborationNotification
        {
            RecipientUserId = recipientUserId,
            NotificationType = "ApprovalDecision",
            TriggeredByUserName = approverName,
            Title = approved ? $"{approverName} approved your changes" : $"{approverName} rejected your changes",
            Message = reason,
            ProjectId = projectId,
            RelatedResourceId = approvalId,
            RelatedResourceType = "ApprovalDecision",
            ActionUrl = actionUrl
        };

        notification.Metadata["approved"] = approved;

        await AddNotificationAsync(notification);
        await BroadcastToSubscribersAsync(recipientUserId, notification);
    }

    public async Task SendActivityNotificationAsync(string recipientUserId, string userId, string activityType, 
        string description, string projectId, string? actionUrl = null)
    {
        var notification = new CollaborationNotification
        {
            RecipientUserId = recipientUserId,
            NotificationType = "Activity",
            TriggeredByUserName = userId,
            Title = FormatActivityTitle(activityType),
            Message = description,
            ProjectId = projectId,
            ActionUrl = actionUrl
        };

        await AddNotificationAsync(notification);
        await BroadcastToSubscribersAsync(recipientUserId, notification);
    }

    public async Task SendBatchNotificationsAsync(List<CollaborationNotification> notifications)
    {
        foreach (var notification in notifications)
        {
            await AddNotificationAsync(notification);
            await BroadcastToSubscribersAsync(notification.RecipientUserId, notification);
        }
    }

    public async Task BroadcastNotificationAsync(List<string> userIds, CollaborationNotification notification)
    {
        foreach (var userId in userIds)
        {
            var userNotification = new CollaborationNotification
            {
                Id = Guid.NewGuid().ToString(),
                RecipientUserId = userId,
                NotificationType = notification.NotificationType,
                TriggeredByUserId = notification.TriggeredByUserId,
                TriggeredByUserName = notification.TriggeredByUserName,
                Title = notification.Title,
                Message = notification.Message,
                ProjectId = notification.ProjectId,
                RelatedResourceId = notification.RelatedResourceId,
                RelatedResourceType = notification.RelatedResourceType,
                ActionUrl = notification.ActionUrl,
                Metadata = notification.Metadata
            };

            await AddNotificationAsync(userNotification);
            await BroadcastToSubscribersAsync(userId, userNotification);
        }
    }

    public async Task<List<CollaborationNotification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        lock (_lockObject)
        {
            if (!_userNotifications.TryGetValue(userId, out var notifications))
            {
                return new();
            }

            var result = unreadOnly
                ? notifications.Where(n => !n.IsRead).ToList()
                : notifications.ToList();

            return result.OrderByDescending(n => n.CreatedAt).ToList();
        }
    }

    public async Task MarkNotificationAsDeliveredAsync(string notificationId)
    {
        lock (_lockObject)
        {
            foreach (var notifications in _userNotifications.Values)
            {
                var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    // Delivery tracked in metadata
                    notification.Metadata["deliveredAt"] = DateTime.UtcNow;
                    return;
                }
            }
        }

        await Task.CompletedTask;
    }

    public async Task MarkNotificationAsReadAsync(string notificationId)
    {
        lock (_lockObject)
        {
            foreach (var notifications in _userNotifications.Values)
            {
                var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    return;
                }
            }
        }

        await Task.CompletedTask;
    }

    public async Task DeleteNotificationAsync(string notificationId)
    {
        lock (_lockObject)
        {
            foreach (var notifications in _userNotifications.Values)
            {
                notifications.RemoveAll(n => n.Id == notificationId);
            }
        }

        await Task.CompletedTask;
    }

    public async Task ClearUserNotificationsAsync(string userId)
    {
        lock (_lockObject)
        {
            _userNotifications[userId] = new();
        }

        await Task.CompletedTask;
    }

    public async Task<NotificationStats> GetNotificationStatsAsync(string userId)
    {
        lock (_lockObject)
        {
            var notifications = GetUserNotificationsAsync(userId, false).Result;

            var stats = new NotificationStats
            {
                UserId = userId,
                TotalNotifications = notifications.Count,
                UnreadCount = notifications.Count(n => !n.IsRead),
                UndeliveredCount = notifications.Count(n => !n.Metadata.ContainsKey("deliveredAt")),
                OldestNotificationDate = notifications.LastOrDefault()?.CreatedAt
            };

            // Count by type
            foreach (var notification in notifications)
            {
                if (!stats.NotificationsByType.ContainsKey(notification.NotificationType))
                {
                    stats.NotificationsByType[notification.NotificationType] = 0;
                }
                stats.NotificationsByType[notification.NotificationType]++;
            }

            return stats;
        }
    }

    public IDisposable SubscribeToNotifications(string userId, Func<CollaborationNotification, Task> handler)
    {
        lock (_lockObject)
        {
            if (!_subscriptions.ContainsKey(userId))
            {
                _subscriptions[userId] = new();
            }
            _subscriptions[userId].Add(handler);
        }

        return new NotificationSubscription(userId, handler, _subscriptions);
    }

    // ===== PRIVATE HELPERS =====

    private async Task AddNotificationAsync(CollaborationNotification notification)
    {
        lock (_lockObject)
        {
            if (!_userNotifications.ContainsKey(notification.RecipientUserId))
            {
                _userNotifications[notification.RecipientUserId] = new();
            }

            _userNotifications[notification.RecipientUserId].Insert(0, notification);

            // Keep only last 100 notifications per user
            if (_userNotifications[notification.RecipientUserId].Count > 100)
            {
                _userNotifications[notification.RecipientUserId].RemoveRange(100, 
                    _userNotifications[notification.RecipientUserId].Count - 100);
            }
        }

        await Task.CompletedTask;
    }

    private async Task BroadcastToSubscribersAsync(string userId, CollaborationNotification notification)
    {
        lock (_lockObject)
        {
            if (_subscriptions.TryGetValue(userId, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    _ = handler(notification).ContinueWith(_ => { }); // Fire and forget
                }
            }
        }

        await Task.CompletedTask;
    }

    private string FormatActivityTitle(string activityType)
    {
        return activityType switch
        {
            "ModelCreated" => "🤖 Model created",
            "ModelTrained" => "🎓 Model trained",
            "ModelDeployed" => "🚀 Model deployed",
            "DatasetCleaned" => "🧹 Dataset cleaned",
            "PipelineExecuted" => "⚙️ Pipeline executed",
            _ => $"📝 {activityType}"
        };
    }

    private class NotificationSubscription : IDisposable
    {
        private readonly string _userId;
        private readonly Func<CollaborationNotification, Task> _handler;
        private readonly Dictionary<string, List<Func<CollaborationNotification, Task>>> _subscriptions;

        public NotificationSubscription(string userId, 
            Func<CollaborationNotification, Task> handler,
            Dictionary<string, List<Func<CollaborationNotification, Task>>> subscriptions)
        {
            _userId = userId;
            _handler = handler;
            _subscriptions = subscriptions;
        }

        public void Dispose()
        {
            lock (_subscriptions)
            {
                if (_subscriptions.TryGetValue(_userId, out var handlers))
                {
                    handlers.Remove(_handler);
                }
            }
        }
    }
}

/// <summary>
/// SignalR-based push notification delivery for web clients.
/// </summary>
public class SignalRNotificationService : IRealTimeNotificationService
{
    // TODO: Implement using SignalR hubs to push notifications in real-time
    // This would integrate with CollaborationHub to send notifications
    // directly to connected clients

    public Task SendMentionNotificationAsync(string recipientUserId, string mentionedBy, string message, 
        string projectId, string resourceId, string? actionUrl = null)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task SendApprovalNotificationAsync(string recipientUserId, string requestedBy, string approvalId, 
        string changeDescription, string projectId, string? actionUrl = null)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task SendCommentNotificationAsync(string recipientUserId, string commentedBy, string commentId, 
        string preview, string resourceId, string projectId, string? actionUrl = null)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task SendEditAlertAsync(string recipientUserId, string editorName, string resourceId, 
        string resourceName, string projectId, string? actionUrl = null)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task SendConflictNotificationAsync(string recipientUserId, List<string> affectedUserIds, 
        string resourceId, string conflictSummary, string projectId, string? actionUrl = null)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task SendApprovalDecisionAsync(string recipientUserId, string approverName, string approvalId, 
        bool approved, string reason, string projectId, string? actionUrl = null)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task SendActivityNotificationAsync(string recipientUserId, string userId, string activityType, 
        string description, string projectId, string? actionUrl = null)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task SendBatchNotificationsAsync(List<CollaborationNotification> notifications)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task BroadcastNotificationAsync(List<string> userIds, CollaborationNotification notification)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task<List<CollaborationNotification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task MarkNotificationAsDeliveredAsync(string notificationId)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task MarkNotificationAsReadAsync(string notificationId)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task DeleteNotificationAsync(string notificationId)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task ClearUserNotificationsAsync(string userId)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public Task<NotificationStats> GetNotificationStatsAsync(string userId)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }

    public IDisposable SubscribeToNotifications(string userId, Func<CollaborationNotification, Task> handler)
    {
        throw new NotImplementedException("SignalR implementation pending");
    }
}

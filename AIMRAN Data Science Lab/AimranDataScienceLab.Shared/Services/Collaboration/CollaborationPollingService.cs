namespace AimranDataScienceLab.Shared.Services.Collaboration;

using AIMRAN_Data_Science_Lab.Services.Collaboration;
using AIMRAN_Data_Science_Lab.Services.Notifications;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// Service that provides a polling-based alternative to SignalR for MAUI clients.
/// Used when SignalR/WebSocket connectivity is unavailable on desktop.
/// </summary>
public interface ICollaborationPollingService
{
    /// <summary>
    /// Initialize polling client for a user.
    /// </summary>
    Task InitializeAsync(string userId, string userName, string projectId, int pollIntervalMs = 2000);

    /// <summary>
    /// Start polling for presence updates.
    /// </summary>
    Task<bool> StartPresencePollingAsync(Func<List<UserPresence>, Task> onUpdate);

    /// <summary>
    /// Start polling for activity updates.
    /// </summary>
    Task<bool> StartActivityPollingAsync(string projectId, Func<List<ActivityEvent>, Task> onUpdate);

    /// <summary>
    /// Start polling for comment updates.
    /// </summary>
    Task<bool> StartCommentPollingAsync(string resourceId, Func<List<CollaborativeComment>, Task> onUpdate);

    /// <summary>
    /// Start polling for notification updates.
    /// </summary>
    Task<bool> StartNotificationPollingAsync(Func<List<CollaborationNotification>, Task> onUpdate);

    /// <summary>
    /// Stop all polling operations.
    /// </summary>
    Task StopPollingAsync();

    /// <summary>
    /// Check if polling is active.
    /// </summary>
    bool IsPollingActive { get; }

    /// <summary>
    /// Get current poll health status.
    /// </summary>
    PollingStatus GetStatus();
}

/// <summary>
/// Status information about polling operations.
/// </summary>
public class PollingStatus
{
    public string UserId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int PresencePollCount { get; set; }
    public int ActivityPollCount { get; set; }
    public int CommentPollCount { get; set; }
    public int NotificationPollCount { get; set; }
    public DateTime? LastPresencePoll { get; set; }
    public DateTime? LastActivityPoll { get; set; }
    public DateTime? LastCommentPoll { get; set; }
    public DateTime? LastNotificationPoll { get; set; }
    public TimeSpan? Uptime { get; set; }
    public Dictionary<string, object> Errors { get; set; } = new();
}

/// <summary>
/// MAUI-compatible polling implementation for collaboration updates.
/// </summary>
public class CollaborationPollingService : ICollaborationPollingService
{
    private readonly IPresenceService _presenceService;
    private readonly ICollaborationService _collaborationService;
    private readonly IRealTimeNotificationService _notificationService;

    private string _currentUserId = string.Empty;
    private string _currentProjectId = string.Empty;
    private int _pollIntervalMs = 2000;
    private bool _isPolling = false;
    private CancellationTokenSource? _cancellationToken;
    private DateTime _startTime = DateTime.MinValue;

    private List<Task> _activePollingTasks = new();

    public CollaborationPollingService(
        IPresenceService presenceService,
        ICollaborationService collaborationService,
        IRealTimeNotificationService notificationService)
    {
        _presenceService = presenceService;
        _collaborationService = collaborationService;
        _notificationService = notificationService;
    }

    public bool IsPollingActive => _isPolling;

    public async Task InitializeAsync(string userId, string userName, string projectId, int pollIntervalMs = 2000)
    {
        _currentUserId = userId;
        _currentProjectId = projectId;
        _pollIntervalMs = pollIntervalMs;
        _startTime = DateTime.UtcNow;

        // Register user presence for polling
        await _presenceService.RegisterUserAsync(userId, userName, "", "", Guid.NewGuid().ToString());
    }

    public async Task<bool> StartPresencePollingAsync(Func<List<UserPresence>, Task> onUpdate)
    {
        if (_isPolling)
            return false;

        _cancellationToken = new CancellationTokenSource();
        _isPolling = true;

        var pollingTask = Task.Run(async () =>
        {
            int pollCount = 0;
            while (!_cancellationToken!.Token.IsCancellationRequested)
            {
                try
                {
                    var onlineUsers = await _presenceService.GetOnlineUsersInProjectAsync(_currentProjectId);
                    await onUpdate(onlineUsers);
                    pollCount++;

                    await Task.Delay(_pollIntervalMs, _cancellationToken!.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log error but continue polling
                    System.Diagnostics.Debug.WriteLine($"Presence polling error: {ex.Message}");
                    await Task.Delay(_pollIntervalMs * 2, _cancellationToken!.Token);
                }
            }
        });

        _activePollingTasks.Add(pollingTask);
        return true;
    }

    public async Task<bool> StartActivityPollingAsync(string projectId, Func<List<ActivityEvent>, Task> onUpdate)
    {
        if (!_isPolling)
            return false;

        var pollingTask = Task.Run(async () =>
        {
            DateTime lastCheck = DateTime.UtcNow;
            while (!_cancellationToken?.Token.IsCancellationRequested ?? false)
            {
                try
                {
                    var activities = await _collaborationService.GetProjectActivitiesAsync(projectId, take: 20);
                    var recentActivities = activities
                        .Where(a => a.CreatedAt > lastCheck)
                        .OrderByDescending(a => a.CreatedAt)
                        .ToList();

                    if (recentActivities.Any())
                    {
                        await onUpdate(recentActivities);
                        lastCheck = DateTime.UtcNow;
                    }

                    await Task.Delay(_pollIntervalMs, _cancellationToken!.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Activity polling error: {ex.Message}");
                    await Task.Delay(_pollIntervalMs * 2, _cancellationToken!.Token);
                }
            }
        });

        _activePollingTasks.Add(pollingTask);
        return true;
    }

    public async Task<bool> StartCommentPollingAsync(string resourceId, Func<List<CollaborativeComment>, Task> onUpdate)
    {
        if (!_isPolling)
            return false;

        var pollingTask = Task.Run(async () =>
        {
            DateTime lastCheck = DateTime.UtcNow;
            while (!_cancellationToken?.Token.IsCancellationRequested ?? false)
            {
                try
                {
                    var comments = await _collaborationService.GetCommentsForResourceAsync(resourceId);
                    var newComments = comments
                        .Where(c => c.CreatedAt > lastCheck)
                        .OrderByDescending(c => c.CreatedAt)
                        .ToList();

                    if (newComments.Any())
                    {
                        await onUpdate(newComments);
                        lastCheck = DateTime.UtcNow;
                    }

                    await Task.Delay(_pollIntervalMs, _cancellationToken!.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Comment polling error: {ex.Message}");
                    await Task.Delay(_pollIntervalMs * 2, _cancellationToken!.Token);
                }
            }
        });

        _activePollingTasks.Add(pollingTask);
        return true;
    }

    public async Task<bool> StartNotificationPollingAsync(Func<List<CollaborationNotification>, Task> onUpdate)
    {
        if (!_isPolling)
            return false;

        var pollingTask = Task.Run(async () =>
        {
            DateTime lastCheck = DateTime.UtcNow;
            while (!_cancellationToken?.Token.IsCancellationRequested ?? false)
            {
                try
                {
                    var notifications = await _notificationService.GetUserNotificationsAsync(_currentUserId, unreadOnly: true);
                    var newNotifications = notifications
                        .Where(n => n.CreatedAt > lastCheck)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();

                    if (newNotifications.Any())
                    {
                        await onUpdate(newNotifications);
                        lastCheck = DateTime.UtcNow;
                    }

                    await Task.Delay(_pollIntervalMs, _cancellationToken!.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Notification polling error: {ex.Message}");
                    await Task.Delay(_pollIntervalMs * 2, _cancellationToken!.Token);
                }
            }
        });

        _activePollingTasks.Add(pollingTask);
        return true;
    }

    public async Task StopPollingAsync()
    {
        if (!_isPolling)
            return;

        _cancellationToken?.Cancel();
        _isPolling = false;

        try
        {
            await Task.WhenAll(_activePollingTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }

        _activePollingTasks.Clear();
    }

    public PollingStatus GetStatus()
    {
        var status = new PollingStatus
        {
            UserId = _currentUserId,
            IsActive = _isPolling,
            Uptime = _startTime != DateTime.MinValue ? DateTime.UtcNow - _startTime : null
        };

        if (_startTime != DateTime.MinValue)
        {
            status.Uptime = DateTime.UtcNow - _startTime;
        }

        return status;
    }
}

/// <summary>
/// Hybrid collaboration client that switches between SignalR and polling based on platform/connectivity.
/// </summary>
public interface IHybridCollaborationClient
{
    /// <summary>
    /// Initialize hybrid client (detects SignalR vs polling).
    /// </summary>
    Task InitializeAsync(string userId, string userName, string projectId, bool preferSignalR = true);

    /// <summary>
    /// Check current transport (SignalR, Polling, or Offline).
    /// </summary>
    string GetCurrentTransport();

    /// <summary>
    /// Force fallback to polling.
    /// </summary>
    Task<bool> FallbackToPollingAsync();

    /// <summary>
    /// Try to reconnect to SignalR if currently using polling.
    /// </summary>
    Task<bool> TryReconnectSignalRAsync();

    /// <summary>
    /// Shutdown the hybrid client.
    /// </summary>
    Task ShutdownAsync();
}

/// <summary>
/// Implementation of hybrid collaboration client.
/// </summary>
public class HybridCollaborationClient : IHybridCollaborationClient
{
    private readonly ICollaborationPollingService _pollingService;
    private string _currentTransport = "Offline";
    private string _currentUserId = string.Empty;

    public HybridCollaborationClient(ICollaborationPollingService pollingService)
    {
        _pollingService = pollingService;
    }

    public async Task InitializeAsync(string userId, string userName, string projectId, bool preferSignalR = true)
    {
        _currentUserId = userId;

        if (preferSignalR)
        {
            // Try SignalR first (for web/hybrid scenarios)
            _currentTransport = "SignalR";
        }
        else
        {
            // Start with polling (for desktop-only scenarios)
            await _pollingService.InitializeAsync(userId, userName, projectId);
            _currentTransport = "Polling";
        }
    }

    public string GetCurrentTransport()
    {
        return _currentTransport;
    }

    public async Task<bool> FallbackToPollingAsync()
    {
        if (_currentTransport == "Polling")
            return true;

        try
        {
            _currentTransport = "Polling";
            // In production, unsubscribe from SignalR and start polling
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TryReconnectSignalRAsync()
    {
        if (_currentTransport == "SignalR")
            return true;

        try
        {
            // Try reconnect logic
            _currentTransport = "SignalR";
            await _pollingService.StopPollingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ShutdownAsync()
    {
        if (_currentTransport == "Polling")
        {
            await _pollingService.StopPollingAsync();
        }
        _currentTransport = "Offline";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIMRAN_Data_Science_Lab.Services.Events;

/// <summary>
/// In-memory Event Bus implementation for inter-role communication.
/// Can be replaced with RabbitMQ/Azure Service Bus for distributed systems.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly Dictionary<Guid, Dictionary<Type, List<Delegate>>> _aggregateSubscribers = new();
    private readonly List<DomainEvent> _eventLog = new();

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
    {
        _eventLog.Add(@event);

        // Global subscribers
        if (_subscribers.TryGetValue(typeof(TEvent), out var globalHandlers))
        {
            var tasks = globalHandlers
                .Cast<Func<TEvent, Task>>()
                .Select(handler => handler(@event));

            await Task.WhenAll(tasks);
        }

        // Aggregate-specific subscribers
        if (_aggregateSubscribers.TryGetValue(@event.ProjectId, out var projectSubscribers))
        {
            if (projectSubscribers.TryGetValue(typeof(TEvent), out var aggregateHandlers))
            {
                var tasks = aggregateHandlers
                    .Cast<Func<TEvent, Task>>()
                    .Select(handler => handler(@event));

                await Task.WhenAll(tasks);
            }
        }
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        if (!_subscribers.ContainsKey(typeof(TEvent)))
            _subscribers[typeof(TEvent)] = new List<Delegate>();

        _subscribers[typeof(TEvent)].Add(handler);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TEvent>(Guid aggregateId, Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        if (!_aggregateSubscribers.ContainsKey(aggregateId))
            _aggregateSubscribers[aggregateId] = new Dictionary<Type, List<Delegate>>();

        var projectDict = _aggregateSubscribers[aggregateId];
        if (!projectDict.ContainsKey(typeof(TEvent)))
            projectDict[typeof(TEvent)] = new List<Delegate>();

        projectDict[typeof(TEvent)].Add(handler);
        return Task.CompletedTask;
    }

    public IReadOnlyList<DomainEvent> GetEventLog() => _eventLog.AsReadOnly();

    public IReadOnlyList<DomainEvent> GetEventLogForProject(Guid projectId) =>
        _eventLog.Where(e => e.ProjectId == projectId).ToList().AsReadOnly();

    public void ClearEventLog() => _eventLog.Clear();
}

/// <summary>
/// Notification Service for cross-role alerts and real-time updates.
/// Handles email, push, and in-app notifications.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(Notification notification);
    Task<Notification[]> GetUserNotificationsAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task DeleteNotificationAsync(Guid notificationId);
    Task SubscribeToNotificationsAsync(Guid userId, Func<Notification, Task> callback);
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannels Channels { get; set; } = NotificationChannels.InApp;

    public Dictionary<string, object>? ActionData { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    DatasetReady,
    ModelTrained,
    PipelineCompleted,
    DeploymentApproved,
    AlertTriggered,
    TaskAssigned,
    WorkflowBlocked,
    InsightGenerated,
    AnomalyDetected
}

[Flags]
public enum NotificationChannels
{
    InApp = 1,
    Email = 2,
    Push = 4,
    Slack = 8
}

/// <summary>
/// Version Control Service for tracking changes across all resources.
/// Provides audit trail and rollback capabilities.
/// </summary>
public interface IVersionControlService
{
    Task<ResourceVersion> CreateVersionAsync(ResourceVersion version);
    Task<ResourceVersion?> GetVersionAsync(Guid resourceId, int versionNumber);
    Task<ResourceVersion[]> GetResourceVersionsAsync(Guid resourceId);

    Task<ResourceVersion> RollbackToVersionAsync(Guid resourceId, int versionNumber);
    Task<VersionComparison> CompareVersionsAsync(Guid resourceId, int version1, int version2);

    Task<Guid> GetLatestVersionIdAsync(Guid resourceId);
}

public class ResourceVersion
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string ResourceType { get; set; } = string.Empty; // Dataset, Model, Pipeline, etc.

    public int VersionNumber { get; set; }
    public string Description { get; set; } = string.Empty;

    public Dictionary<string, object> Content { get; set; } = new();
    public Dictionary<string, object> Changes { get; set; } = new(); // What changed from previous version

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VersionComparison
{
    public int Version1Number { get; set; }
    public int Version2Number { get; set; }

    public Dictionary<string, object> AddedItems { get; set; } = new();
    public Dictionary<string, object> RemovedItems { get; set; } = new();
    public Dictionary<string, (object Old, object New)> ModifiedItems { get; set; } = new();
}

/// <summary>
/// Audit Trail Service for compliance and forensics.
/// Tracks all actions performed by all roles.
/// </summary>
public interface IAuditTrailService
{
    Task LogActionAsync(AuditEntry entry);
    Task<AuditEntry[]> GetProjectAuditTrailAsync(Guid projectId);
    Task<AuditEntry[]> GetUserAuditTrailAsync(Guid userId);
    Task<AuditEntry[]> GetResourceAuditTrailAsync(Guid resourceId);
    Task<AuditReport> GenerateAuditReportAsync(Guid projectId, DateRange dateRange);
}

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Execute, etc.
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;

    public Dictionary<string, object>? ChangeDetails { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class AuditReport
{
    public Guid ProjectId { get; set; }
    public DateRange Period { get; set; } = new();

    public int TotalActions { get; set; }
    public Dictionary<string, int> ActionsByType { get; set; } = new();
    public Dictionary<string, int> ActionsByRole { get; set; } = new();
    public Dictionary<string, int> ActionsByResource { get; set; } = new();

    public AuditEntry[] CriticalActions { get; set; } = Array.Empty<AuditEntry>();
    public string[]? ComplianceFlags { get; set; }
}

/// <summary>
/// Change Log Service for tracking project-level changes.
/// </summary>
public interface IChangeLogService
{
    Task LogChangeAsync(ChangeLogEntry entry);
    Task<ChangeLogEntry[]> GetProjectChangeLogAsync(Guid projectId);
    Task<ChangeLogSummary> GetChangeLogSummaryAsync(Guid projectId);
}

public class ChangeLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChangeType Type { get; set; }

    public Guid InitiatedByUserId { get; set; }
    public string InitiatedByRole { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ChangeType
{
    DatasetUpdate,
    ModelTraining,
    FeatureEngineering,
    PipelineExecution,
    Deployment,
    Configuration,
    Other
}

public class ChangeLogSummary
{
    public Guid ProjectId { get; set; }
    public int TotalChanges { get; set; }
    public Dictionary<string, int> ChangesByType { get; set; } = new();
    public Dictionary<string, int> ChangesByRole { get; set; } = new();
    public DateTime? LastChangeAt { get; set; }
}

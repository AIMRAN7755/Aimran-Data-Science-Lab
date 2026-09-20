using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AimranDataScienceLab.Core.Services.Administration;

/// <summary>
/// User Management Service for administrators.
/// Manages users, roles, permissions, and team assignments.
/// </summary>
public interface IUserManagementService
{
    Task<AdminUser> CreateUserAsync(AdminUser user);
    Task<AdminUser?> GetUserAsync(Guid userId);
    Task<AdminUser[]> GetAllUsersAsync();
    Task UpdateUserAsync(AdminUser user);
    Task DeleteUserAsync(Guid userId);
    Task DeactivateUserAsync(Guid userId);

    Task AssignRoleToUserAsync(Guid userId, Guid roleId);
    Task RemoveRoleFromUserAsync(Guid userId, Guid roleId);
    Task<AdminUserRole[]> GetUserRolesAsync(Guid userId);
}

public class AdminUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public Guid[] RoleIds { get; set; } = Array.Empty<Guid>();
    public Guid[] ProjectIds { get; set; } = Array.Empty<Guid>();

    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}

public class AdminUserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// System Monitoring Service for administrators.
/// Tracks system health, resource usage, and performance.
/// </summary>
public interface ISystemMonitoringService
{
    Task<SystemHealth> GetSystemHealthAsync();
    Task<ResourceUtilization> GetResourceUtilizationAsync();
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    Task<BackgroundJobStatus[]> GetBackgroundJobsStatusAsync();

    Task<SystemAlert[]> GetActiveAlertsAsync();
    Task<SystemAlert[]> GetSystemAlertHistoryAsync(int limit = 100);
}

public class SystemHealth
{
    public HealthState OverallHealth { get; set; }
    public Dictionary<string, HealthState> ComponentHealth { get; set; } = new();
    public string[] IssuesDetected { get; set; } = Array.Empty<string>();
    public DateTime CheckedAt { get; set; }
}

public enum HealthState
{
    Healthy,
    Degraded,
    Unhealthy
}

public class ResourceUtilization
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double DiskUsagePercent { get; set; }

    public int ActiveUsers { get; set; }
    public int RunningPipelines { get; set; }
    public int PendingTasks { get; set; }

    public DateTime MeasuredAt { get; set; }
}

public class PerformanceMetrics
{
    public double AverageApiLatencyMs { get; set; }
    public int QueriesPerSecond { get; set; }
    public double DatabaseConnectionPoolUsagePercent { get; set; }
    public int CacheHitRate { get; set; }
}

public class BackgroundJobStatus
{
    public string JobName { get; set; } = string.Empty;
    public JobState State { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? NextExecutionAt { get; set; }
    public string? LastError { get; set; }
}

public enum JobState
{
    Idle,
    Running,
    Completed,
    Failed
}

public class SystemAlert
{
    public Guid Id { get; set; }
    public AlertLevel Level { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public enum AlertLevel
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Audit Log Service for administrators.
/// Provides detailed audit trail viewing and reporting.
/// </summary>
public interface IAuditLogService
{
    Task<AuditLogEntry[]> GetAuditLogsAsync(AuditLogFilter filter);
    Task<AuditLogStatistics> GetAuditStatisticsAsync(Guid projectId);
    Task<AuditLogReport> GenerateAuditReportAsync(AuditReportRequest request);
}

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;

    public Dictionary<string, object>? Details { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public DateTime Timestamp { get; set; }
}

public class AuditLogFilter
{
    public Guid? ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public string? ResourceType { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int? StatusCode { get; set; }
    public int? Limit { get; set; } = 100;
}

public class AuditLogStatistics
{
    public Guid ProjectId { get; set; }
    public int TotalActions { get; set; }
    public Dictionary<string, int> ActionsByType { get; set; } = new();
    public Dictionary<string, int> ActionsByUser { get; set; } = new();
    public Dictionary<string, int> ActionsByResource { get; set; } = new();
}

public class AuditLogReport
{
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateRange Period { get; set; } = new();

    public AuditLogEntry[] Entries { get; set; } = Array.Empty<AuditLogEntry>();
    public AuditLogStatistics Statistics { get; set; } = new();
    public string[] ComplianceFlags { get; set; } = Array.Empty<string>();
}

public class AuditReportRequest
{
    public Guid ProjectId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string[]? IncludeActions { get; set; }
    public string? ExportFormat { get; set; } // "PDF", "CSV", "JSON"
}

public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// System Configuration Service for administrators.
/// Manages platform-wide settings and policies.
/// </summary>
public interface ISystemConfigurationService
{
    Task<SystemConfiguration> GetConfigurationAsync();
    Task UpdateConfigurationAsync(SystemConfiguration config);

    Task<SecurityPolicy> GetSecurityPolicyAsync();
    Task UpdateSecurityPolicyAsync(SecurityPolicy policy);

    Task<EmailConfiguration> GetEmailConfigurationAsync();
    Task UpdateEmailConfigurationAsync(EmailConfiguration config);

    Task<StorageConfiguration> GetStorageConfigurationAsync();
    Task UpdateStorageConfigurationAsync(StorageConfiguration config);
}

public class SystemConfiguration
{
    public string ApplicationName { get; set; } = "AIMRAN Data Science Lab";
    public string ApplicationVersion { get; set; } = string.Empty;

    public bool MaintenanceMode { get; set; }
    public string? MaintenanceMessage { get; set; }

    public int MaxConcurrentPipelines { get; set; } = 5;
    public int MaxConcurrentModels { get; set; } = 10;

    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public class SecurityPolicy
{
    public int PasswordMinLength { get; set; } = 8;
    public bool RequireComplexPassword { get; set; } = true;
    public int PasswordExpiryDays { get; set; } = 90;

    public int SessionTimeoutMinutes { get; set; } = 30;
    public bool EnableMFA { get; set; } = true;

    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;

    public string[] IpWhitelist { get; set; } = Array.Empty<string>();
}

public class EmailConfiguration
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool UseSSL { get; set; } = true;
}

public class StorageConfiguration
{
    public string StorageType { get; set; } = string.Empty; // "Local", "S3", "AzureBlob"
    public string StoragePath { get; set; } = string.Empty;
    public int MaxStorageGB { get; set; }
    public int DataRetentionDays { get; set; } = 365;
}

/// <summary>
/// License and Billing Service for administrators.
/// </summary>
public interface ILicenseService
{
    Task<LicenseInfo> GetLicenseInfoAsync();
    Task<UsageMetrics> GetUsageMetricsAsync();
    Task<BillingInformation> GetBillingInformationAsync();
}

public class LicenseInfo
{
    public string LicenseKey { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProjects { get; set; }
    public bool IsValid { get; set; }
}

public class UsageMetrics
{
    public int ActiveUsers { get; set; }
    public int ActiveProjects { get; set; }
    public long StorageUsedGB { get; set; }
    public int PipelinesExecutedToday { get; set; }
    public int ModelsTrainedToday { get; set; }
}

public class BillingInformation
{
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal MonthlyCharge { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime NextBillingDate { get; set; }
}

using AIMRAN_Data_Science_Lab.Models.Auth;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Records and queries security-relevant events for compliance and monitoring.
/// </summary>
public interface IAuditService
{
    /// <summary>Record a security event.</summary>
    Task LogAsync(AuditLogEntry entry, CancellationToken ct = default);

    /// <summary>Record a security event for the current user with minimal parameters.</summary>
    Task LogAsync(string action, string? detail = null, CancellationToken ct = default);

    /// <summary>Query recent audit log entries.</summary>
    Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int count = 100, CancellationToken ct = default);

    /// <summary>Query audit log entries for a specific user.</summary>
    Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(Guid userId, int count = 50, CancellationToken ct = default);

    /// <summary>Query audit log entries by action type.</summary>
    Task<IReadOnlyList<AuditLogEntry>> GetByActionAsync(string action, int count = 50, CancellationToken ct = default);
}

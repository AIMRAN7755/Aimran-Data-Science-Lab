using AIMRAN_Data_Science_Lab.Models;
using AIMRAN_Data_Science_Lab.Models.Auth;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Administrative service for managing users, storage quotas, and system settings.
/// Requires <see cref="AimranPermission.ManageUsers"/> or <see cref="AimranPermission.ConfigureSystem"/>.
/// </summary>
public interface IAdminService
{
    // ── User Management ─────────────────────────────────────────────────

    /// <summary>Get all registered users.</summary>
    Task<IReadOnlyList<AimranUser>> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>Get a user by ID.</summary>
    Task<AimranUser?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Update a user's role.</summary>
    Task UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default);

    /// <summary>Lock or unlock a user account.</summary>
    Task SetUserLockedAsync(Guid userId, bool locked, CancellationToken ct = default);

    /// <summary>Reset a user's password (admin override).</summary>
    Task ResetUserPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>Delete a user account.</summary>
    Task DeleteUserAsync(Guid userId, CancellationToken ct = default);

    // ── Storage Quotas ──────────────────────────────────────────────────

    /// <summary>Get all storage quotas.</summary>
    Task<IReadOnlyList<StorageQuota>> GetAllQuotasAsync(CancellationToken ct = default);

    /// <summary>Get or create a storage quota for a user.</summary>
    Task<StorageQuota> GetQuotaAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Set the maximum storage for a user.</summary>
    Task SetQuotaAsync(Guid userId, long maxStorageBytes, CancellationToken ct = default);

    /// <summary>Recalculate used storage for a user.</summary>
    Task<long> RecalculateUsedStorageAsync(Guid userId, CancellationToken ct = default);

    // ── System Settings ─────────────────────────────────────────────────

    /// <summary>Get all system settings.</summary>
    Task<IReadOnlyList<SystemSetting>> GetAllSettingsAsync(CancellationToken ct = default);

    /// <summary>Get a setting value by key.</summary>
    Task<string?> GetSettingAsync(string key, CancellationToken ct = default);

    /// <summary>Create or update a setting.</summary>
    Task SetSettingAsync(string key, string value, string? description = null, CancellationToken ct = default);

    /// <summary>Delete a setting.</summary>
    Task DeleteSettingAsync(string key, CancellationToken ct = default);

    // ── System Overview ─────────────────────────────────────────────────

    /// <summary>Get aggregated system statistics for the admin dashboard.</summary>
    Task<AdminDashboardStats> GetDashboardStatsAsync(CancellationToken ct = default);
}

/// <summary>
/// Storage quota for a user.
/// </summary>
public record StorageQuota
{
    public Guid UserId { get; init; }
    public string? Username { get; init; }
    public long MaxStorageBytes { get; init; }
    public long UsedStorageBytes { get; init; }
    public double UsagePercent => MaxStorageBytes > 0 ? (double)UsedStorageBytes / MaxStorageBytes * 100 : 0;
}

/// <summary>
/// A key-value system configuration setting.
/// </summary>
public record SystemSetting
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Aggregated statistics shown on the admin dashboard.
/// </summary>
public record AdminDashboardStats
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int LockedUsers { get; init; }
    public int TotalProjects { get; init; }
    public int TotalDatasets { get; init; }
    public int TotalModels { get; init; }
    public int TotalExperiments { get; init; }
    public long TotalStorageUsedBytes { get; init; }
    public int AuditEventsToday { get; init; }
}

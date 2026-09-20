using AIMRAN_Data_Science_Lab.Models.Auth;
using AimranDataScienceLab.Engine.Data;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// SQLite-backed admin service for user management, storage quotas, and system settings.
/// </summary>
internal sealed class LocalAdminService : IAdminService
{
    private readonly SqliteConnectionFactory _db;
    private readonly ILogger<LocalAdminService> _logger;

    public LocalAdminService(SqliteConnectionFactory db, ILogger<LocalAdminService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── User Management ──────────────────────────────────────────────

    public Task<IReadOnlyList<AimranUser>> GetAllUsersAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT user_id, username, password_hash, email, role,
                   is_locked, failed_login_attempts, locked_until,
                   created_at, last_login_at
            FROM users ORDER BY created_at DESC;
            """;

        var users = new List<AimranUser>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            users.Add(ReadUser(reader));

        return Task.FromResult<IReadOnlyList<AimranUser>>(users);
    }

    public Task<AimranUser?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT user_id, username, password_hash, email, role,
                   is_locked, failed_login_attempts, locked_until,
                   created_at, last_login_at
            FROM users WHERE user_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", userId.ToString());

        using var reader = cmd.ExecuteReader();
        return Task.FromResult(reader.Read() ? ReadUser(reader) : null);
    }

    public Task UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default)
    {
        if (!AimranRoles.All.Contains(newRole))
            throw new ArgumentException($"Unknown role: {newRole}", nameof(newRole));

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE users SET role = $r WHERE user_id = $id;";
        cmd.Parameters.AddWithValue("$r", newRole);
        cmd.Parameters.AddWithValue("$id", userId.ToString());
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Admin updated role for {UserId} to {Role}", userId, newRole);
        return Task.CompletedTask;
    }

    public Task SetUserLockedAsync(Guid userId, bool locked, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE users
            SET is_locked = $l, locked_until = $lu, failed_login_attempts = 0
            WHERE user_id = $id;
            """;
        cmd.Parameters.AddWithValue("$l", locked ? 1 : 0);
        cmd.Parameters.AddWithValue("$lu", locked
            ? DateTime.UtcNow.AddYears(100).ToString("O")
            : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$id", userId.ToString());
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Admin {Action} user {UserId}", locked ? "locked" : "unlocked", userId);
        return Task.CompletedTask;
    }

    public Task ResetUserPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.", nameof(newPassword));

        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE users
            SET password_hash = $h, failed_login_attempts = 0, is_locked = 0, locked_until = NULL
            WHERE user_id = $id;
            """;
        cmd.Parameters.AddWithValue("$h", hash);
        cmd.Parameters.AddWithValue("$id", userId.ToString());
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Admin reset password for {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM users WHERE user_id = $id;";
        cmd.Parameters.AddWithValue("$id", userId.ToString());
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Admin deleted user {UserId}", userId);
        return Task.CompletedTask;
    }

    // ── Storage Quotas ───────────────────────────────────────────────

    public Task<IReadOnlyList<StorageQuota>> GetAllQuotasAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT sq.user_id, u.username, sq.max_storage_bytes, sq.used_storage_bytes
            FROM storage_quotas sq
            LEFT JOIN users u ON u.user_id = sq.user_id
            ORDER BY sq.used_storage_bytes DESC;
            """;

        var quotas = new List<StorageQuota>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            quotas.Add(new StorageQuota
            {
                UserId = Guid.Parse(reader.GetString(0)),
                Username = reader.IsDBNull(1) ? null : reader.GetString(1),
                MaxStorageBytes = reader.GetInt64(2),
                UsedStorageBytes = reader.GetInt64(3)
            });
        }

        return Task.FromResult<IReadOnlyList<StorageQuota>>(quotas);
    }

    public Task<StorageQuota> GetQuotaAsync(Guid userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT sq.user_id, u.username, sq.max_storage_bytes, sq.used_storage_bytes
            FROM storage_quotas sq
            LEFT JOIN users u ON u.user_id = sq.user_id
            WHERE sq.user_id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", userId.ToString());

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return Task.FromResult(new StorageQuota
            {
                UserId = Guid.Parse(reader.GetString(0)),
                Username = reader.IsDBNull(1) ? null : reader.GetString(1),
                MaxStorageBytes = reader.GetInt64(2),
                UsedStorageBytes = reader.GetInt64(3)
            });
        }

        // Auto-create with default 100 GB
        const long defaultQuota = 100L * 1024 * 1024 * 1024;
        using var insert = conn.CreateCommand();
        insert.CommandText = """
            INSERT INTO storage_quotas (user_id, max_storage_bytes, used_storage_bytes)
            VALUES ($id, $max, 0);
            """;
        insert.Parameters.AddWithValue("$id", userId.ToString());
        insert.Parameters.AddWithValue("$max", defaultQuota);
        insert.ExecuteNonQuery();

        return Task.FromResult(new StorageQuota
        {
            UserId = userId,
            MaxStorageBytes = defaultQuota,
            UsedStorageBytes = 0
        });
    }

    public Task SetQuotaAsync(Guid userId, long maxStorageBytes, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO storage_quotas (user_id, max_storage_bytes, used_storage_bytes)
            VALUES ($id, $max, 0)
            ON CONFLICT(user_id) DO UPDATE SET max_storage_bytes = $max;
            """;
        cmd.Parameters.AddWithValue("$id", userId.ToString());
        cmd.Parameters.AddWithValue("$max", maxStorageBytes);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<long> RecalculateUsedStorageAsync(Guid userId, CancellationToken ct = default)
    {
        // Sum file sizes from datasets (simplified — real impl would scan disk)
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(size_bytes), 0) FROM datasets;";
        var used = Convert.ToInt64(cmd.ExecuteScalar());

        using var upd = conn.CreateCommand();
        upd.CommandText = """
            UPDATE storage_quotas SET used_storage_bytes = $u WHERE user_id = $id;
            """;
        upd.Parameters.AddWithValue("$u", used);
        upd.Parameters.AddWithValue("$id", userId.ToString());
        upd.ExecuteNonQuery();

        return Task.FromResult(used);
    }

    // ── System Settings ──────────────────────────────────────────────

    public Task<IReadOnlyList<SystemSetting>> GetAllSettingsAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT key, value, description, updated_at FROM system_settings ORDER BY key;";

        var settings = new List<SystemSetting>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            settings.Add(new SystemSetting
            {
                Key = reader.GetString(0),
                Value = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                UpdatedAt = DateTime.Parse(reader.GetString(3))
            });
        }

        return Task.FromResult<IReadOnlyList<SystemSetting>>(settings);
    }

    public Task<string?> GetSettingAsync(string key, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM system_settings WHERE key = $k;";
        cmd.Parameters.AddWithValue("$k", key);
        var result = cmd.ExecuteScalar();
        return Task.FromResult(result as string);
    }

    public Task SetSettingAsync(string key, string value, string? description = null, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO system_settings (key, value, description, updated_at)
            VALUES ($k, $v, $d, $t)
            ON CONFLICT(key) DO UPDATE SET value = $v, description = $d, updated_at = $t;
            """;
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.Parameters.AddWithValue("$d", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task DeleteSettingAsync(string key, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM system_settings WHERE key = $k;";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    // ── Dashboard Stats ──────────────────────────────────────────────

    public Task<AdminDashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        int Scalar(string sql)
        {
            using var c = conn.CreateCommand();
            c.CommandText = sql;
            return Convert.ToInt32(c.ExecuteScalar());
        }

        long ScalarLong(string sql)
        {
            using var c = conn.CreateCommand();
            c.CommandText = sql;
            return Convert.ToInt64(c.ExecuteScalar());
        }

        var stats = new AdminDashboardStats
        {
            TotalUsers = Scalar("SELECT COUNT(*) FROM users;"),
            ActiveUsers = Scalar("SELECT COUNT(*) FROM users WHERE is_locked = 0;"),
            LockedUsers = Scalar("SELECT COUNT(*) FROM users WHERE is_locked = 1;"),
            TotalProjects = Scalar("SELECT COUNT(*) FROM projects;"),
            TotalDatasets = Scalar("SELECT COUNT(*) FROM datasets;"),
            TotalModels = Scalar("SELECT COUNT(*) FROM models;"),
            TotalExperiments = Scalar("SELECT COUNT(*) FROM experiments;"),
            TotalStorageUsedBytes = ScalarLong("SELECT COALESCE(SUM(size_bytes), 0) FROM datasets;"),
            AuditEventsToday = Scalar(
                $"SELECT COUNT(*) FROM security_audit_logs WHERE timestamp >= '{DateTime.UtcNow.Date:O}';")
        };

        return Task.FromResult(stats);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static AimranUser ReadUser(Microsoft.Data.Sqlite.SqliteDataReader reader) =>
        new()
        {
            UserId = Guid.Parse(reader.GetString(0)),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
            Role = reader.GetString(4),
            IsLocked = reader.GetInt32(5) != 0,
            FailedLoginAttempts = reader.GetInt32(6),
            LockedUntil = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
            CreatedAt = DateTime.Parse(reader.GetString(8)),
            LastLoginAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9))
        };
}

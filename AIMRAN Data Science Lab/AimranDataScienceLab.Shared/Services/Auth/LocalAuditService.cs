using AIMRAN_Data_Science_Lab.Models.Auth;
using AimranDataScienceLab.Engine.Data;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// SQLite-backed audit service that records and queries security events.
/// </summary>
internal sealed class LocalAuditService : IAuditService
{
    private readonly SqliteConnectionFactory _db;
    private readonly IAuthService _auth;
    private readonly ILogger<LocalAuditService> _logger;

    public LocalAuditService(
        SqliteConnectionFactory db,
        IAuthService auth,
        ILogger<LocalAuditService> logger)
    {
        _db = db;
        _auth = auth;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? detail = null, CancellationToken ct = default)
    {
        var user = await _auth.GetCurrentUserAsync(ct);
        await LogAsync(new AuditLogEntry
        {
            UserId = user?.UserId,
            Action = action,
            Detail = detail
        }, ct);
    }

    public Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO security_audit_logs (id, user_id, action, detail, timestamp, ip_address)
            VALUES ($id, $uid, $a, $d, $t, $ip);
            """;
        cmd.Parameters.AddWithValue("$id", entry.Id.ToString());
        cmd.Parameters.AddWithValue("$uid", (object?)entry.UserId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$a", entry.Action);
        cmd.Parameters.AddWithValue("$d", (object?)entry.Detail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$t", entry.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("$ip", (object?)entry.IpAddress ?? DBNull.Value);
        cmd.ExecuteNonQuery();

        _logger.LogDebug("Audit: {Action} user={UserId} detail={Detail}",
            entry.Action, entry.UserId, entry.Detail);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int count = 100, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, user_id, action, detail, timestamp, ip_address
            FROM security_audit_logs
            ORDER BY timestamp DESC
            LIMIT $n;
            """;
        cmd.Parameters.AddWithValue("$n", count);
        return Task.FromResult(ReadEntries(cmd));
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(Guid userId, int count = 50, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, user_id, action, detail, timestamp, ip_address
            FROM security_audit_logs
            WHERE user_id = $uid
            ORDER BY timestamp DESC
            LIMIT $n;
            """;
        cmd.Parameters.AddWithValue("$uid", userId.ToString());
        cmd.Parameters.AddWithValue("$n", count);
        return Task.FromResult(ReadEntries(cmd));
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetByActionAsync(string action, int count = 50, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, user_id, action, detail, timestamp, ip_address
            FROM security_audit_logs
            WHERE action = $a
            ORDER BY timestamp DESC
            LIMIT $n;
            """;
        cmd.Parameters.AddWithValue("$a", action);
        cmd.Parameters.AddWithValue("$n", count);
        return Task.FromResult(ReadEntries(cmd));
    }

    private static IReadOnlyList<AuditLogEntry> ReadEntries(Microsoft.Data.Sqlite.SqliteCommand cmd)
    {
        var results = new List<AuditLogEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new AuditLogEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                UserId = reader.IsDBNull(1) ? null : Guid.Parse(reader.GetString(1)),
                Action = reader.GetString(2),
                Detail = reader.IsDBNull(3) ? null : reader.GetString(3),
                Timestamp = DateTime.Parse(reader.GetString(4)),
                IpAddress = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return results;
    }
}

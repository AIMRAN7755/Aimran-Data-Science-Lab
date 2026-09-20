using AIMRAN_Data_Science_Lab.Models.Auth;
using AIMRAN_Data_Science_Lab.Services.Auth;
using AimranDataScienceLab.Engine.Data;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// SQLite-backed authentication service using bcrypt password hashing
/// and JWT token generation. Supports offline / local lab mode.
/// </summary>
internal sealed class LocalAuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly SqliteConnectionFactory _db;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<LocalAuthService> _logger;

    private string? _currentToken;
    private AimranUser? _currentUser;

    public AuthMode Mode => AuthMode.Local;

    public event Action? AuthStateChanged;

    public LocalAuthService(
        SqliteConnectionFactory db,
        IJwtTokenService jwt,
        ILogger<LocalAuthService> logger)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
    }

    public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var conn = _db.CreateConnection();

        // Look up user
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT user_id, username, password_hash, email, role,
                   is_locked, failed_login_attempts, locked_until,
                   created_at, last_login_at
            FROM users WHERE username = $u;
            """;
        cmd.Parameters.AddWithValue("$u", request.Username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            LogAudit(conn, null, "LoginFailed", $"Unknown user: {request.Username}");
            return Task.FromResult(AuthResult.Failure("Invalid username or password."));
        }

        var user = ReadUser(reader);

        // Check lockout
        if (user.IsLocked && user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            LogAudit(conn, user.UserId, "LoginBlocked", "Account locked");
            return Task.FromResult(AuthResult.Failure("Account is locked. Try again later."));
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            var attempts = user.FailedLoginAttempts + 1;
            var lockNow = attempts >= MaxFailedAttempts;

            using var upd = conn.CreateCommand();
            upd.CommandText = """
                UPDATE users
                SET failed_login_attempts = $a,
                    is_locked = $l,
                    locked_until = $lu
                WHERE user_id = $id;
                """;
            upd.Parameters.AddWithValue("$a", attempts);
            upd.Parameters.AddWithValue("$l", lockNow ? 1 : 0);
            upd.Parameters.AddWithValue("$lu",
                lockNow ? DateTime.UtcNow.Add(LockoutDuration).ToString("O") : (object)DBNull.Value);
            upd.Parameters.AddWithValue("$id", user.UserId.ToString());
            upd.ExecuteNonQuery();

            LogAudit(conn, user.UserId, "LoginFailed", $"Attempt {attempts}");
            _logger.LogWarning("Login failed for {User}, attempt {Attempt}", request.Username, attempts);

            return Task.FromResult(AuthResult.Failure(
                lockNow
                    ? "Account locked after too many failed attempts."
                    : "Invalid username or password."));
        }

        // Success — reset lockout and update last login
        using var success = conn.CreateCommand();
        success.CommandText = """
            UPDATE users
            SET failed_login_attempts = 0, is_locked = 0, locked_until = NULL,
                last_login_at = $now
            WHERE user_id = $id;
            """;
        success.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        success.Parameters.AddWithValue("$id", user.UserId.ToString());
        success.ExecuteNonQuery();

        user = user with
        {
            FailedLoginAttempts = 0,
            IsLocked = false,
            LockedUntil = null,
            LastLoginAt = DateTime.UtcNow
        };

        var token = _jwt.GenerateToken(user);
        _currentToken = token;
        _currentUser = user;

        LogAudit(conn, user.UserId, "LoginSuccess", null);
        _logger.LogInformation("User {User} logged in", request.Username);
        AuthStateChanged?.Invoke();

        return Task.FromResult(AuthResult.Success(token, user));
    }

    public Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return Task.FromResult(AuthResult.Failure("Username must be at least 3 characters."));

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Task.FromResult(AuthResult.Failure("Password must be at least 8 characters."));

        if (!AimranRoles.All.Contains(request.Role))
            return Task.FromResult(AuthResult.Failure($"Unknown role: {request.Role}"));

        using var conn = _db.CreateConnection();

        // Check uniqueness
        using var check = conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM users WHERE username = $u;";
        check.Parameters.AddWithValue("$u", request.Username);
        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            return Task.FromResult(AuthResult.Failure("Username already exists."));

        var userId = Guid.NewGuid();
        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var now = DateTime.UtcNow;

        using var insert = conn.CreateCommand();
        insert.CommandText = """
            INSERT INTO users (user_id, username, password_hash, email, role, created_at)
            VALUES ($id, $u, $h, $e, $r, $c);
            """;
        insert.Parameters.AddWithValue("$id", userId.ToString());
        insert.Parameters.AddWithValue("$u", request.Username);
        insert.Parameters.AddWithValue("$h", hash);
        insert.Parameters.AddWithValue("$e", (object?)request.Email ?? DBNull.Value);
        insert.Parameters.AddWithValue("$r", request.Role);
        insert.Parameters.AddWithValue("$c", now.ToString("O"));
        insert.ExecuteNonQuery();

        var user = new AimranUser
        {
            UserId = userId,
            Username = request.Username,
            PasswordHash = hash,
            Email = request.Email,
            Role = request.Role,
            CreatedAt = now
        };

        var token = _jwt.GenerateToken(user);
        _currentToken = token;
        _currentUser = user;

        LogAudit(conn, userId, "Register", $"Role={request.Role}");
        _logger.LogInformation("User {User} registered with role {Role}", request.Username, request.Role);
        AuthStateChanged?.Invoke();

        return Task.FromResult(AuthResult.Success(token, user));
    }

    public Task LogoutAsync(CancellationToken ct = default)
    {
        if (_currentUser is not null)
        {
            _logger.LogInformation("User {User} logged out", _currentUser.Username);
            using var conn = _db.CreateConnection();
            LogAudit(conn, _currentUser.UserId, "Logout", null);
        }

        _currentToken = null;
        _currentUser = null;
        AuthStateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task<AimranUser?> GetCurrentUserAsync(CancellationToken ct = default) =>
        Task.FromResult(_currentUser);

    public Task<bool> IsInRoleAsync(string role, CancellationToken ct = default) =>
        Task.FromResult(_currentUser is not null &&
                        string.Equals(_currentUser.Role, role, StringComparison.OrdinalIgnoreCase));

    public Task<AimranUser?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var principal = _jwt.ValidateToken(token);
        if (principal is null)
            return Task.FromResult<AimranUser?>(null);

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                       ?? principal.FindFirst("sub");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Task.FromResult<AimranUser?>(null);

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

    // ── Helpers ─────────────────────────────────────────────────────────

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

    private static void LogAudit(
        Microsoft.Data.Sqlite.SqliteConnection conn,
        Guid? userId,
        string action,
        string? detail)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO security_audit_logs (id, user_id, action, detail, timestamp)
            VALUES ($id, $uid, $a, $d, $t);
            """;
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("$uid", (object?)userId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$a", action);
        cmd.Parameters.AddWithValue("$d", (object?)detail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }
}

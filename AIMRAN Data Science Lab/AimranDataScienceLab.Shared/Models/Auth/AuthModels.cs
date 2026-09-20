namespace AIMRAN_Data_Science_Lab.Models.Auth;

/// <summary>
/// Represents a user in the AIMRAN authentication system.
/// </summary>
public record AimranUser
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public string? Email { get; init; }
    public required string Role { get; init; }
    public bool IsLocked { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime? LockedUntil { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; init; }
}

/// <summary>
/// Supported authentication modes for the AIMRAN Data Science Lab.
/// </summary>
public enum AuthMode
{
    /// <summary>Local SQLite-backed identity with bcrypt password hashing.</summary>
    Local,

    /// <summary>Microsoft Entra ID (Azure AD) cloud-based identity.</summary>
    AzureEntraId
}

/// <summary>
/// Roles supported by the AIMRAN RBAC system.
/// </summary>
public static class AimranRoles
{
    public const string Admin = "Admin";
    public const string DataScientist = "DataScientist";
    public const string DataEngineer = "DataEngineer";
    public const string AIEngineer = "AIEngineer";
    public const string Analyst = "Analyst";
    public const string Viewer = "Viewer";

    /// <summary>All defined roles in priority order (highest first).</summary>
    public static readonly IReadOnlyList<string> All =
        [Admin, DataScientist, DataEngineer, AIEngineer, Analyst, Viewer];
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public record AuthResult
{
    public bool Succeeded { get; init; }
    public string? Token { get; init; }
    public AimranUser? User { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResult Success(string token, AimranUser user) =>
        new() { Succeeded = true, Token = token, User = user };

    public static AuthResult Failure(string error) =>
        new() { Succeeded = false, ErrorMessage = error };
}

/// <summary>
/// An audit log entry for security-relevant events.
/// </summary>
public record AuditLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? UserId { get; init; }
    public required string Action { get; init; }
    public string? Detail { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? IpAddress { get; init; }
}

/// <summary>
/// Credentials submitted for local login.
/// </summary>
public record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// Request to register a new local user.
/// </summary>
public record RegisterRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string? Email { get; init; }
    public string Role { get; init; } = AimranRoles.Viewer;
}

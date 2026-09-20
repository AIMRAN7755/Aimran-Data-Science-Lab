using AIMRAN_Data_Science_Lab.Models.Auth;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Core authentication service supporting local and cloud identity modes.
/// </summary>
public interface IAuthService
{
    /// <summary>The authentication mode currently in use.</summary>
    AuthMode Mode { get; }

    /// <summary>Authenticate with local credentials.</summary>
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Register a new local user (local mode only).</summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Log out the current user and invalidate the session.</summary>
    Task LogoutAsync(CancellationToken ct = default);

    /// <summary>Get the currently authenticated user, or null.</summary>
    Task<AimranUser?> GetCurrentUserAsync(CancellationToken ct = default);

    /// <summary>Check whether the current user has a given role.</summary>
    Task<bool> IsInRoleAsync(string role, CancellationToken ct = default);

    /// <summary>Validate a JWT token and return the associated user.</summary>
    Task<AimranUser?> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Raised when authentication state changes (login/logout).</summary>
    event Action? AuthStateChanged;
}

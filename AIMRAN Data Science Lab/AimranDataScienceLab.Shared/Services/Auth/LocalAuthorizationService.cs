using AIMRAN_Data_Science_Lab.Models.Auth;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// Permission-based authorization using the role-permission matrix.
/// </summary>
internal sealed class LocalAuthorizationService : IAuthorizationService
{
    private readonly IAuthService _auth;
    private readonly ILogger<LocalAuthorizationService> _logger;

    public LocalAuthorizationService(IAuthService auth, ILogger<LocalAuthorizationService> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(AimranPermission permission, CancellationToken ct = default)
    {
        var user = await _auth.GetCurrentUserAsync(ct);
        if (user is null)
            return false;

        return RolePermissions.HasPermission(user.Role, permission);
    }

    public async Task RequirePermissionAsync(AimranPermission permission, CancellationToken ct = default)
    {
        var user = await _auth.GetCurrentUserAsync(ct);
        if (user is null)
        {
            _logger.LogWarning("Permission {Permission} denied — no authenticated user", permission);
            throw new UnauthorizedAccessException("Authentication required.");
        }

        if (!RolePermissions.HasPermission(user.Role, permission))
        {
            _logger.LogWarning(
                "Permission {Permission} denied for user {User} (role={Role})",
                permission, user.Username, user.Role);
            throw new UnauthorizedAccessException(
                $"Permission '{permission}' is required. Your role '{user.Role}' does not grant it.");
        }
    }

    public async Task<IReadOnlySet<AimranPermission>> GetCurrentPermissionsAsync(CancellationToken ct = default)
    {
        var user = await _auth.GetCurrentUserAsync(ct);
        if (user is null)
            return new HashSet<AimranPermission>();

        return RolePermissions.GetPermissions(user.Role);
    }
}

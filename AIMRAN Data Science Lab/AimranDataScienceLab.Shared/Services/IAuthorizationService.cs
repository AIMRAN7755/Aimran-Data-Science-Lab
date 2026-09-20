using AIMRAN_Data_Science_Lab.Models.Auth;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Checks whether the current user is authorized to perform specific actions.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>Check if the current user has a specific permission.</summary>
    Task<bool> HasPermissionAsync(AimranPermission permission, CancellationToken ct = default);

    /// <summary>
    /// Require a permission, throwing <see cref="UnauthorizedAccessException"/> if denied.
    /// </summary>
    Task RequirePermissionAsync(AimranPermission permission, CancellationToken ct = default);

    /// <summary>Get all permissions granted to the current user.</summary>
    Task<IReadOnlySet<AimranPermission>> GetCurrentPermissionsAsync(CancellationToken ct = default);
}

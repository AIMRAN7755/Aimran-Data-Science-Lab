using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.RoleManagement;

namespace AimranDataScienceLab.Core.Services.RoleManagement;

/// <summary>
/// Manages roles, permissions, and authorization across the platform.
/// Implements role-based access control (RBAC) for all 4 data science roles.
/// </summary>
public interface IRoleService
{
    // Role Management
    Task<ProfessionalRole> CreateRoleAsync(ProfessionalRole role);
    Task<ProfessionalRole?> GetRoleAsync(Guid roleId);
    Task<ProfessionalRole[]> GetAllRolesAsync();
    Task UpdateRoleAsync(ProfessionalRole role);
    Task DeleteRoleAsync(Guid roleId);

    // User Role Assignment
    Task AssignRoleToUserAsync(Guid userId, Guid roleId);
    Task RemoveRoleFromUserAsync(Guid userId, Guid roleId);
    Task<ProfessionalRole[]> GetUserRolesAsync(Guid userId);

    // Permission Management
    Task<bool> HasPermissionAsync(Guid userId, string resource, PermissionAction action);
    Task<bool> HasAnyPermissionAsync(Guid userId, string resource, PermissionAction[] actions);
    Task<bool> HasAllPermissionsAsync(Guid userId, string resource, PermissionAction[] actions);

    // Permission Scopes
    Task<PermissionScope[]> GetPermissionScopesAsync(Guid roleId);
    Task GrantPermissionScopeAsync(Guid roleId, PermissionScope scope);
    Task RevokePermissionScopeAsync(Guid roleId, string scope);

    // Workflow Capabilities
    Task<string[]> GetAllowedWorkflowsForRoleAsync(Guid roleId);
    Task<string[]> GetAllowedToolsForRoleAsync(Guid roleId);

    // Default Roles
    Task<ProfessionalRole> GetOrCreateDataAnalystRoleAsync();
    Task<ProfessionalRole> GetOrCreateDataScientistRoleAsync();
    Task<ProfessionalRole> GetOrCreateDataEngineerRoleAsync();
    Task<ProfessionalRole> GetOrCreateAIEngineerRoleAsync();
}

/// <summary>
/// Represents a permission scope for fine-grained access control.
/// </summary>
public class PermissionScope
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PermissionAction[] Actions { get; set; } = Array.Empty<PermissionAction>();
    public string? Condition { get; set; }
}

/// <summary>
/// Authorization service to check permissions.
/// </summary>
public interface IAuthorizationService
{
    Task<bool> CanUserAccessProjectAsync(Guid userId, Guid projectId);
    Task<bool> CanUserAccessDatasetAsync(Guid userId, Guid datasetId);
    Task<bool> CanUserAccessModelAsync(Guid userId, Guid modelId);
    Task<bool> CanUserManagePipelineAsync(Guid userId, Guid pipelineId);

    Task<bool> CanUserApproveInsightAsync(Guid userId, Guid insightId);
    Task<bool> CanUserDeployModelAsync(Guid userId, Guid modelVersionId);

    Task ThrowIfUnauthorizedAsync(Guid userId, string resource, PermissionAction action);
}

public class RBACContext
{
    public Guid UserId { get; set; }
    public Guid[] RoleIds { get; set; } = Array.Empty<Guid>();
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Claims { get; set; } = new();
}

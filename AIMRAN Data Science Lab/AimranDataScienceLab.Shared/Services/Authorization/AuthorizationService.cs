using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.RoleManagement;
using AimranDataScienceLab.Core.Domain.Projects;
using AimranDataScienceLab.Core.Services.RoleManagement;
using AimranDataScienceLab.Shared.Services.Projects;

namespace AimranDataScienceLab.Core.Services.Authorization;

/// <summary>
/// Authorization service implementing fine-grained access control.
/// Checks if users can perform specific actions on resources based on their roles.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IRoleService _roleService;
    private readonly IProjectService _projectService;

    public AuthorizationService(IRoleService roleService, IProjectService projectService)
    {
        _roleService = roleService;
        _projectService = projectService;
    }

    public async Task<bool> CanUserAccessProjectAsync(Guid userId, Guid projectId)
    {
        var project = await _projectService.GetProjectAsync(projectId);
        if (project == null)
            return false;

        // Owner always has access
        if (project.OwnerId == userId)
            return true;

        // Team members have access
        return project.TeamMemberIds.Contains(userId);
    }

    public async Task<bool> CanUserAccessDatasetAsync(Guid userId, Guid datasetId)
    {
        var hasReadPermission = await _roleService.HasPermissionAsync(
            userId, "datasets", PermissionAction.Read);

        if (!hasReadPermission)
            return false;

        // Check if user is in the project that owns the dataset
        // This would require additional context, simplified here
        return true;
    }

    public async Task<bool> CanUserAccessModelAsync(Guid userId, Guid modelId)
    {
        var hasReadPermission = await _roleService.HasPermissionAsync(
            userId, "models", PermissionAction.Read);

        return hasReadPermission;
    }

    public async Task<bool> CanUserManagePipelineAsync(Guid userId, Guid pipelineId)
    {
        var roles = await _roleService.GetUserRolesAsync(userId);
        var isDataEngineer = roles.Any(r => r.Type == RoleType.DataEngineer);

        if (!isDataEngineer)
            return false;

        var hasManagePermission = await _roleService.HasPermissionAsync(
            userId, "pipelines", PermissionAction.Update);

        return hasManagePermission;
    }

    public async Task<bool> CanUserApproveInsightAsync(Guid userId, Guid insightId)
    {
        var roles = await _roleService.GetUserRolesAsync(userId);
        var isAnalyst = roles.Any(r => r.Type == RoleType.DataAnalyst);
        var isScientist = roles.Any(r => r.Type == RoleType.DataScientist);

        return isAnalyst || isScientist;
    }

    public async Task<bool> CanUserDeployModelAsync(Guid userId, Guid modelVersionId)
    {
        var roles = await _roleService.GetUserRolesAsync(userId);
        var isAIEngineer = roles.Any(r => r.Type == RoleType.AIEngineer);

        if (!isAIEngineer)
            return false;

        var hasDeployPermission = await _roleService.HasPermissionAsync(
            userId, "deployments", PermissionAction.Create);

        return hasDeployPermission;
    }

    public async Task ThrowIfUnauthorizedAsync(Guid userId, string resource, PermissionAction action)
    {
        var hasPermission = await _roleService.HasPermissionAsync(userId, resource, action);
        if (!hasPermission)
            throw new UnauthorizedAccessException(
                $"User {userId} does not have permission to {action} on {resource}");
    }
}

public interface IProjectService
{
    Task<DataScienceProject?> GetProjectAsync(Guid projectId);
}

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

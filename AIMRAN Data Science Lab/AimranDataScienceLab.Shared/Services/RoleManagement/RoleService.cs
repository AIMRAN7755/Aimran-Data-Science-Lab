using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.RoleManagement;

namespace AimranDataScienceLab.Core.Services.RoleManagement;

/// <summary>
/// Implementation of role-based access control (RBAC) service.
/// Provides centralized permission management for all 4 data science roles.
/// </summary>
public class RoleService : IRoleService
{
    private readonly Dictionary<Guid, ProfessionalRole> _roles = new();
    private readonly Dictionary<Guid, List<Guid>> _userRoles = new(); // UserId -> RoleIds
    private readonly Dictionary<Guid, List<RolePermission>> _rolePermissions = new();

    public RoleService()
    {
        InitializeDefaultRoles();
    }

    private void InitializeDefaultRoles()
    {
        // Data Analyst Role
        var analystRole = new ProfessionalRole
        {
            Id = Guid.NewGuid(),
            Name = "Data Analyst",
            Description = "Focuses on SQL queries, dashboard creation, reporting, and business insights",
            Type = RoleType.DataAnalyst,
            Priority = 1,
            PermissionScopes = new[] { "datasets:read", "dashboards:*", "reports:*", "sql:execute" },
            AllowedWorkflows = new[] { "EDA", "Reporting", "DashboardCreation" },
            AllowedTools = new[] { "SQLEditor", "DashboardBuilder", "ReportGenerator", "DataProfiler" }
        };
        _roles[analystRole.Id] = analystRole;

        // Data Scientist Role
        var scientistRole = new ProfessionalRole
        {
            Id = Guid.NewGuid(),
            Name = "Data Scientist",
            Description = "Focuses on ML models, feature engineering, experimentation, and model versioning",
            Type = RoleType.DataScientist,
            Priority = 2,
            PermissionScopes = new[] { "datasets:read", "models:*", "experiments:*", "features:*" },
            AllowedWorkflows = new[] { "FeatureEngineering", "ModelBuilding", "Experimentation" },
            AllowedTools = new[] { "MLStudio", "FeatureLab", "ModelRegistry", "ExperimentTracker" }
        };
        _roles[scientistRole.Id] = scientistRole;

        // Data Engineer Role
        var engineerRole = new ProfessionalRole
        {
            Id = Guid.NewGuid(),
            Name = "Data Engineer",
            Description = "Focuses on pipelines, data integration, ETL, and data connectors",
            Type = RoleType.DataEngineer,
            Priority = 3,
            PermissionScopes = new[] { "pipelines:*", "connectors:*", "datasets:write", "etl:*" },
            AllowedWorkflows = new[] { "DataIngestion", "DataTransformation", "PipelineExecution" },
            AllowedTools = new[] { "PipelineBuilder", "DataConnectors", "ETLEngine", "Scheduler" }
        };
        _roles[engineerRole.Id] = engineerRole;

        // AI Engineer Role
        var aiRole = new ProfessionalRole
        {
            Id = Guid.NewGuid(),
            Name = "AI Engineer",
            Description = "Focuses on model deployment, APIs, model serving, and monitoring",
            Type = RoleType.AIEngineer,
            Priority = 4,
            PermissionScopes = new[] { "models:read", "deployments:*", "apis:*", "monitoring:*" },
            AllowedWorkflows = new[] { "ModelDeployment", "APIManagement", "Monitoring" },
            AllowedTools = new[] { "DeploymentCenter", "APIManager", "MonitoringDashboard", "LoadBalancer" }
        };
        _roles[aiRole.Id] = aiRole;
    }

    public Task<ProfessionalRole> CreateRoleAsync(ProfessionalRole role)
    {
        role.Id = Guid.NewGuid();
        role.CreatedAt = DateTime.UtcNow;
        role.UpdatedAt = DateTime.UtcNow;
        _roles[role.Id] = role;
        return Task.FromResult(role);
    }

    public Task<ProfessionalRole?> GetRoleAsync(Guid roleId)
    {
        return Task.FromResult(_roles.TryGetValue(roleId, out var role) ? role : null);
    }

    public Task<ProfessionalRole[]> GetAllRolesAsync()
    {
        return Task.FromResult(_roles.Values.ToArray());
    }

    public Task UpdateRoleAsync(ProfessionalRole role)
    {
        if (_roles.ContainsKey(role.Id))
        {
            role.UpdatedAt = DateTime.UtcNow;
            _roles[role.Id] = role;
        }
        return Task.CompletedTask;
    }

    public Task DeleteRoleAsync(Guid roleId)
    {
        _roles.Remove(roleId);
        _rolePermissions.Remove(roleId);
        return Task.CompletedTask;
    }

    public Task AssignRoleToUserAsync(Guid userId, Guid roleId)
    {
        if (!_userRoles.ContainsKey(userId))
            _userRoles[userId] = new List<Guid>();

        if (!_userRoles[userId].Contains(roleId))
            _userRoles[userId].Add(roleId);

        return Task.CompletedTask;
    }

    public Task RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        if (_userRoles.TryGetValue(userId, out var roles))
        {
            roles.Remove(roleId);
        }
        return Task.CompletedTask;
    }

    public Task<ProfessionalRole[]> GetUserRolesAsync(Guid userId)
    {
        if (!_userRoles.TryGetValue(userId, out var roleIds))
            return Task.FromResult(Array.Empty<ProfessionalRole>());

        var roles = roleIds
            .Where(rid => _roles.ContainsKey(rid))
            .Select(rid => _roles[rid])
            .ToArray();

        return Task.FromResult(roles);
    }

    public Task<bool> HasPermissionAsync(Guid userId, string resource, PermissionAction action)
    {
        if (!_userRoles.TryGetValue(userId, out var roleIds))
            return Task.FromResult(false);

        foreach (var roleId in roleIds)
        {
            if (_roles.TryGetValue(roleId, out var role))
            {
                if (role.PermissionScopes.Any(ps => MatchesPermission(ps, resource, action)))
                    return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> HasAnyPermissionAsync(Guid userId, string resource, PermissionAction[] actions)
    {
        foreach (var action in actions)
        {
            if (HasPermissionAsync(userId, resource, action).Result)
                return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> HasAllPermissionsAsync(Guid userId, string resource, PermissionAction[] actions)
    {
        foreach (var action in actions)
        {
            if (!HasPermissionAsync(userId, resource, action).Result)
                return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    public Task<PermissionScope[]> GetPermissionScopesAsync(Guid roleId)
    {
        if (!_roles.TryGetValue(roleId, out var role))
            return Task.FromResult(Array.Empty<PermissionScope>());

        var scopes = role.PermissionScopes
            .Select(ps => new PermissionScope { Name = ps })
            .ToArray();

        return Task.FromResult(scopes);
    }

    public Task GrantPermissionScopeAsync(Guid roleId, PermissionScope scope)
    {
        if (_roles.TryGetValue(roleId, out var role))
        {
            var currentScopes = role.PermissionScopes.ToList();
            if (!currentScopes.Contains(scope.Name))
            {
                currentScopes.Add(scope.Name);
                role.PermissionScopes = currentScopes.ToArray();
            }
        }
        return Task.CompletedTask;
    }

    public Task RevokePermissionScopeAsync(Guid roleId, string scope)
    {
        if (_roles.TryGetValue(roleId, out var role))
        {
            var currentScopes = role.PermissionScopes.ToList();
            currentScopes.Remove(scope);
            role.PermissionScopes = currentScopes.ToArray();
        }
        return Task.CompletedTask;
    }

    public Task<string[]> GetAllowedWorkflowsForRoleAsync(Guid roleId)
    {
        if (_roles.TryGetValue(roleId, out var role))
            return Task.FromResult(role.AllowedWorkflows);

        return Task.FromResult(Array.Empty<string>());
    }

    public Task<string[]> GetAllowedToolsForRoleAsync(Guid roleId)
    {
        if (_roles.TryGetValue(roleId, out var role))
            return Task.FromResult(role.AllowedTools);

        return Task.FromResult(Array.Empty<string>());
    }

    public async Task<ProfessionalRole> GetOrCreateDataAnalystRoleAsync()
    {
        var role = _roles.Values.FirstOrDefault(r => r.Type == RoleType.DataAnalyst);
        return role ?? new ProfessionalRole { Type = RoleType.DataAnalyst };
    }

    public async Task<ProfessionalRole> GetOrCreateDataScientistRoleAsync()
    {
        var role = _roles.Values.FirstOrDefault(r => r.Type == RoleType.DataScientist);
        return role ?? new ProfessionalRole { Type = RoleType.DataScientist };
    }

    public async Task<ProfessionalRole> GetOrCreateDataEngineerRoleAsync()
    {
        var role = _roles.Values.FirstOrDefault(r => r.Type == RoleType.DataEngineer);
        return role ?? new ProfessionalRole { Type = RoleType.DataEngineer };
    }

    public async Task<ProfessionalRole> GetOrCreateAIEngineerRoleAsync()
    {
        var role = _roles.Values.FirstOrDefault(r => r.Type == RoleType.AIEngineer);
        return role ?? new ProfessionalRole { Type = RoleType.AIEngineer };
    }

    private bool MatchesPermission(string permissionScope, string resource, PermissionAction action)
    {
        // Parse permission scope (e.g., "datasets:read", "dashboards:*")
        var parts = permissionScope.Split(':');
        if (parts.Length != 2) return false;

        var scopeResource = parts[0];
        var scopeAction = parts[1];

        if (scopeResource != resource && scopeResource != "*")
            return false;

        if (scopeAction == "*")
            return true;

        return scopeAction.Equals(action.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

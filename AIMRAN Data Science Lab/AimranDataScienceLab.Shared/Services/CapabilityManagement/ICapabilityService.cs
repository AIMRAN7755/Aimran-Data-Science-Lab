using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.RoleManagement;

namespace AimranDataScienceLab.Core.Services.CapabilityManagement;

/// <summary>
/// Manages feature capabilities and access control across roles.
/// Enables fine-grained capability distribution beyond Settings.
/// </summary>
public interface ICapabilityService
{
    /// <summary>
    /// Check if a user role has a specific capability.
    /// </summary>
    Task<bool> HasCapabilityAsync(Guid userId, string capabilityId);

    /// <summary>
    /// Get all capabilities for a specific role.
    /// </summary>
    Task<string[]> GetCapabilitiesForRoleAsync(RoleType roleType);

    /// <summary>
    /// Get capability details including description and enabled state.
    /// </summary>
    Task<CapabilityDefinition?> GetCapabilityAsync(string capabilityId);

    /// <summary>
    /// Get all available capabilities.
    /// </summary>
    Task<CapabilityDefinition[]> GetAllCapabilitiesAsync();

    /// <summary>
    /// Check if a capability is available for a given role.
    /// </summary>
    Task<bool> IsCapabilityAvailableForRoleAsync(RoleType roleType, string capabilityId);
}

/// <summary>
/// Represents a feature capability that can be gated by role.
/// </summary>
public class CapabilityDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Collaboration, Experimentation, DataManagement, Pipeline, Deployment
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Lab capability categories distributed across roles.
/// </summary>
public static class CapabilityCategories
{
    public const string Collaboration = "Collaboration";
    public const string LiveEditing = "LiveEditing";
    public const string Commenting = "Commenting";
    public const string Experimentation = "Experimentation";
    public const string DataManagement = "DataManagement";
    public const string PipelineManagement = "PipelineManagement";
    public const string ModelManagement = "ModelManagement";
    public const string Deployment = "Deployment";
    public const string Monitoring = "Monitoring";
    public const string Administration = "Administration";
}

/// <summary>
/// Predefined capabilities mapped to roles.
/// </summary>
public static class LabCapabilities
{
    // ─── COLLABORATION CAPABILITIES ───
    public const string ViewCollaborationDashboard = "collaboration:view-dashboard";
    public const string ViewActivityFeed = "collaboration:view-activity-feed";
    public const string ViewTeamPresence = "collaboration:view-team-presence";
    public const string JoinLiveSession = "collaboration:join-live-session";

    // ─── LIVE EDITING CAPABILITIES ───
    public const string EditLive = "liveediting:edit";
    public const string ViewOtherCursors = "liveediting:view-cursors";
    public const string ViewConflictResolution = "liveediting:view-conflicts";
    public const string ResolveConflicts = "liveediting:resolve-conflicts";

    // ─── COMMENTING CAPABILITIES ───
    public const string AddComments = "commenting:add";
    public const string ViewComments = "commenting:view";
    public const string ReplyToComments = "commenting:reply";
    public const string ResolveComments = "commenting:resolve";
    public const string ManageComments = "commenting:manage";
    public const string MentionUsers = "commenting:mention";

    // ─── EXPERIMENTATION CAPABILITIES ───
    public const string ViewExperiments = "experimentation:view";
    public const string RunExperiments = "experimentation:run";
    public const string CreateExperiments = "experimentation:create";
    public const string CompareExperiments = "experimentation:compare";
    public const string ManageExperiments = "experimentation:manage";

    // ─── DATA MANAGEMENT CAPABILITIES ───
    public const string ViewDatasets = "data:view-datasets";
    public const string ImportDatasets = "data:import";
    public const string ExportDatasets = "data:export";
    public const string ProfileData = "data:profile";
    public const string ManageDatasets = "data:manage";

    // ─── PIPELINE CAPABILITIES ───
    public const string ViewPipelines = "pipeline:view";
    public const string ExecutePipelines = "pipeline:execute";
    public const string CreatePipelines = "pipeline:create";
    public const string EditPipelines = "pipeline:edit";
    public const string ManagePipelines = "pipeline:manage";
    public const string ConfigureIntegrations = "pipeline:configure-integrations";
    public const string MonitorPipelines = "pipeline:monitor";

    // ─── MODEL MANAGEMENT CAPABILITIES ───
    public const string ViewModels = "model:view";
    public const string TrainModels = "model:train";
    public const string CreateModels = "model:create";
    public const string EvaluateModels = "model:evaluate";
    public const string VersionModels = "model:version";
    public const string ManageModels = "model:manage";

    // ─── DEPLOYMENT CAPABILITIES ───
    public const string ViewDeployments = "deployment:view";
    public const string DeployModels = "deployment:deploy";
    public const string ConfigureDeployments = "deployment:configure";
    public const string ManageDeployments = "deployment:manage";
    public const string OptimizeDeployments = "deployment:optimize";
    public const string ManageAPIs = "deployment:manage-apis";

    // ─── MONITORING CAPABILITIES ───
    public const string ViewMetrics = "monitoring:view-metrics";
    public const string ViewLogs = "monitoring:view-logs";
    public const string MonitorPerformance = "monitoring:monitor";
    public const string ManageAlerts = "monitoring:manage-alerts";

    // ─── ADMINISTRATION CAPABILITIES ───
    public const string ManageUsers = "admin:manage-users";
    public const string ViewAuditLogs = "admin:view-audit-logs";
    public const string ManageRoles = "admin:manage-roles";
    public const string ConfigureSystem = "admin:configure-system";
}

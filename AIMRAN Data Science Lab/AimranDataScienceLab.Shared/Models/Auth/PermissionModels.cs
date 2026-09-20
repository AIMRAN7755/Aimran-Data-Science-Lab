namespace AIMRAN_Data_Science_Lab.Models.Auth;

/// <summary>
/// Granular permissions that can be checked against the current user's role.
/// </summary>
public enum AimranPermission
{
    // ── Project ─────────────────────────────────────────────────────────
    ViewProjects,
    CreateProject,
    DeleteProject,

    // ── Dataset ─────────────────────────────────────────────────────────
    ViewDatasets,
    ImportDataset,
    DeleteDataset,
    ExportDataset,

    // ── Experiments & Models ────────────────────────────────────────────
    ViewExperiments,
    RunExperiment,
    TrainModel,
    DeployModel,
    DeleteModel,

    // ── Code Execution ─────────────────────────────────────────────────
    ExecutePython,
    ExecuteR,
    ExecuteNotebook,

    // ── Extensions ─────────────────────────────────────────────────────
    ViewExtensions,
    InstallExtension,
    ManageExtensions,

    // ── Administration ─────────────────────────────────────────────────
    ManageUsers,
    ViewAuditLogs,
    ManageSecrets,
    ConfigureSystem,

    // ── Collaboration ─────────────────────────────────────────────────
    ViewCollaborationDashboard,
    ViewActivityFeed,
    ViewTeamPresence,
    JoinLiveSession,

    // ── Live Editing ─────────────────────────────────────────────────
    EditLive,
    ViewOtherCursors,
    ViewConflictResolution,
    ResolveConflicts,

    // ── Commenting ─────────────────────────────────────────────────
    ViewComments,
    AddComments,
    ReplyToComments,
    ResolveComments,
    ManageComments,
    MentionUsers,

    // ── Pipeline Management ─────────────────────────────────────────
    ViewPipelines,
    ExecutePipelines,
    CreatePipelines,
    EditPipelines,
    ManagePipelines,
    ConfigureIntegrations,
    MonitorPipelines,

    // ── Model Management ─────────────────────────────────────────────
    ViewModels,
    CreateModels,
    EvaluateModels,
    VersionModels,
    ManageModels,

    // ── Deployment ─────────────────────────────────────────────────
    ViewDeployments,
    ConfigureDeployments,
    ManageDeployments,
    OptimizeDeployments,
    ManageAPIs,

    // ── Monitoring ─────────────────────────────────────────────────
    ViewMetrics,
    ViewLogs,
    MonitorPerformance,
    ManageAlerts
}

/// <summary>
/// Maps each <see cref="AimranRoles"/> role to the permissions it grants.
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, HashSet<AimranPermission>> s_map = new(StringComparer.OrdinalIgnoreCase)
    {
        [AimranRoles.Viewer] =
        [
            AimranPermission.ViewProjects,
            AimranPermission.ViewDatasets,
            AimranPermission.ViewExperiments,
            AimranPermission.ViewExtensions,
            // Collaboration (Read-Only)
            AimranPermission.ViewCollaborationDashboard,
            AimranPermission.ViewActivityFeed,
            AimranPermission.ViewTeamPresence,
            AimranPermission.JoinLiveSession,
            AimranPermission.ViewComments,
            AimranPermission.ViewOtherCursors,
            AimranPermission.ViewConflictResolution
        ],

        [AimranRoles.Analyst] =
        [
            // inherits Viewer
            AimranPermission.ViewProjects,
            AimranPermission.ViewDatasets,
            AimranPermission.ViewExperiments,
            AimranPermission.ViewExtensions,
            AimranPermission.ViewCollaborationDashboard,
            AimranPermission.ViewActivityFeed,
            AimranPermission.ViewTeamPresence,
            AimranPermission.JoinLiveSession,
            AimranPermission.ViewComments,
            AimranPermission.ViewOtherCursors,
            AimranPermission.ViewConflictResolution,

            // + analyst privileges
            AimranPermission.ImportDataset,
            AimranPermission.ExportDataset,
            AimranPermission.ExecutePython,
            AimranPermission.ExecuteR,
            AimranPermission.ExecuteNotebook,
            // Collaboration (Write)
            AimranPermission.AddComments,
            AimranPermission.ReplyToComments,
            AimranPermission.MentionUsers,
            AimranPermission.ViewPipelines,
            AimranPermission.ViewModels,
            AimranPermission.ViewMetrics
        ],

        [AimranRoles.DataScientist] =
        [
            // inherits Analyst
            AimranPermission.ViewProjects,
            AimranPermission.ViewDatasets,
            AimranPermission.ViewExperiments,
            AimranPermission.ViewExtensions,
            AimranPermission.ViewCollaborationDashboard,
            AimranPermission.ViewActivityFeed,
            AimranPermission.ViewTeamPresence,
            AimranPermission.JoinLiveSession,
            AimranPermission.ViewComments,
            AimranPermission.ViewOtherCursors,
            AimranPermission.ViewConflictResolution,
            AimranPermission.ImportDataset,
            AimranPermission.ExportDataset,
            AimranPermission.ExecutePython,
            AimranPermission.ExecuteR,
            AimranPermission.ExecuteNotebook,
            AimranPermission.AddComments,
            AimranPermission.ReplyToComments,
            AimranPermission.MentionUsers,
            AimranPermission.ViewPipelines,
            AimranPermission.ViewModels,
            AimranPermission.ViewMetrics,

            // + data-scientist privileges
            AimranPermission.CreateProject,
            AimranPermission.RunExperiment,
            AimranPermission.TrainModel,
            AimranPermission.DeployModel,
            AimranPermission.InstallExtension,
            // Extended Collaboration
            AimranPermission.EditLive,
            AimranPermission.ResolveConflicts,
            AimranPermission.ResolveComments,
            AimranPermission.RunExperiment,
            AimranPermission.CreateModels,
            AimranPermission.EvaluateModels,
            AimranPermission.VersionModels
        ],

        [AimranRoles.DataEngineer] =
        [
            // Collaboration (Full)
            AimranPermission.ViewCollaborationDashboard,
            AimranPermission.ViewActivityFeed,
            AimranPermission.ViewTeamPresence,
            AimranPermission.JoinLiveSession,
            AimranPermission.EditLive,
            AimranPermission.ViewOtherCursors,
            AimranPermission.ViewConflictResolution,
            AimranPermission.ResolveConflicts,
            AimranPermission.ViewComments,
            AimranPermission.AddComments,
            AimranPermission.ReplyToComments,
            AimranPermission.ResolveComments,
            AimranPermission.MentionUsers,

            // Data (Full Control)
            AimranPermission.ViewDatasets,
            AimranPermission.ImportDataset,
            AimranPermission.ExportDataset,
            AimranPermission.ExecutePython,
            AimranPermission.ExecuteR,

            // Pipeline (Full Control)
            AimranPermission.ViewPipelines,
            AimranPermission.ExecutePipelines,
            AimranPermission.CreatePipelines,
            AimranPermission.EditPipelines,
            AimranPermission.ManagePipelines,
            AimranPermission.ConfigureIntegrations,
            AimranPermission.MonitorPipelines,

            // Monitoring
            AimranPermission.ViewMetrics,
            AimranPermission.ViewLogs,
            AimranPermission.MonitorPerformance,

            // View Others
            AimranPermission.ViewExperiments,
            AimranPermission.ViewModels,
            AimranPermission.ViewDeployments
        ],

        [AimranRoles.AIEngineer] =
        [
            // Collaboration (Full)
            AimranPermission.ViewCollaborationDashboard,
            AimranPermission.ViewActivityFeed,
            AimranPermission.ViewTeamPresence,
            AimranPermission.JoinLiveSession,
            AimranPermission.EditLive,
            AimranPermission.ViewOtherCursors,
            AimranPermission.ViewConflictResolution,
            AimranPermission.ResolveConflicts,
            AimranPermission.ViewComments,
            AimranPermission.AddComments,
            AimranPermission.ReplyToComments,
            AimranPermission.ResolveComments,
            AimranPermission.ManageComments,
            AimranPermission.MentionUsers,

            // Models (Full Control)
            AimranPermission.ViewModels,
            AimranPermission.TrainModel,
            AimranPermission.CreateModels,
            AimranPermission.EvaluateModels,
            AimranPermission.VersionModels,
            AimranPermission.ManageModels,

            // Deployment (Full Control)
            AimranPermission.ViewDeployments,
            AimranPermission.DeployModel,
            AimranPermission.ConfigureDeployments,
            AimranPermission.ManageDeployments,
            AimranPermission.OptimizeDeployments,
            AimranPermission.ManageAPIs,

            // Monitoring (Full)
            AimranPermission.ViewMetrics,
            AimranPermission.ViewLogs,
            AimranPermission.MonitorPerformance,
            AimranPermission.ManageAlerts,

            // Data & Experiments (View)
            AimranPermission.ViewDatasets,
            AimranPermission.ExportDataset,
            AimranPermission.ViewExperiments,
            AimranPermission.RunExperiment
        ],

        [AimranRoles.Admin] = new(Enum.GetValues<AimranPermission>())
    };

    /// <summary>Check whether a role grants a specific permission.</summary>
    public static bool HasPermission(string role, AimranPermission permission) =>
        s_map.TryGetValue(role, out var perms) && perms.Contains(permission);

    /// <summary>Get all permissions for a role.</summary>
    public static IReadOnlySet<AimranPermission> GetPermissions(string role) =>
        s_map.TryGetValue(role, out var perms) ? perms : new HashSet<AimranPermission>();
}

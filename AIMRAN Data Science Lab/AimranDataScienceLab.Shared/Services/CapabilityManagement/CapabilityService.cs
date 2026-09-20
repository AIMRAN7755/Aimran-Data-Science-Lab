using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.RoleManagement;

namespace AimranDataScienceLab.Core.Services.CapabilityManagement;

/// <summary>
/// Implementation of capability service with role-based access control.
/// Distributes lab capabilities across DataAnalyst, DataScientist, DataEngineer, and AIEngineer roles.
/// </summary>
public class CapabilityService : ICapabilityService
{
    private readonly Dictionary<RoleType, HashSet<string>> _roleCapabilities;
    private readonly Dictionary<string, CapabilityDefinition> _capabilities;

    public CapabilityService()
    {
        _capabilities = InitializeCapabilities();
        _roleCapabilities = InitializeRoleCapabilities();
    }

    /// <summary>
    /// Check if a user role has a specific capability.
    /// This is a simplified version - in production, verify user's assigned roles first.
    /// </summary>
    public Task<bool> HasCapabilityAsync(Guid userId, string capabilityId)
    {
        // TODO: In production, resolve userId to roles, then check if any role has the capability
        return Task.FromResult(false);
    }

    /// <summary>
    /// Get all capabilities for a specific role.
    /// </summary>
    public Task<string[]> GetCapabilitiesForRoleAsync(RoleType roleType)
    {
        _roleCapabilities.TryGetValue(roleType, out var capabilities);
        return Task.FromResult(capabilities?.ToArray() ?? Array.Empty<string>());
    }

    /// <summary>
    /// Get capability details.
    /// </summary>
    public Task<CapabilityDefinition?> GetCapabilityAsync(string capabilityId)
    {
        var found = _capabilities.TryGetValue(capabilityId, out var capability);
        return Task.FromResult(found ? capability : null);
    }

    /// <summary>
    /// Get all available capabilities.
    /// </summary>
    public Task<CapabilityDefinition[]> GetAllCapabilitiesAsync()
    {
        return Task.FromResult(_capabilities.Values.ToArray());
    }

    /// <summary>
    /// Check if a capability is available for a given role.
    /// </summary>
    public Task<bool> IsCapabilityAvailableForRoleAsync(RoleType roleType, string capabilityId)
    {
        _roleCapabilities.TryGetValue(roleType, out var capabilities);
        var isAvailable = capabilities?.Contains(capabilityId) ?? false;
        return Task.FromResult(isAvailable);
    }

    /// <summary>
    /// Initialize all lab capabilities with their metadata.
    /// </summary>
    private Dictionary<string, CapabilityDefinition> InitializeCapabilities()
    {
        return new Dictionary<string, CapabilityDefinition>
        {
            // ─── COLLABORATION ───
            [LabCapabilities.ViewCollaborationDashboard] = new()
            {
                Id = LabCapabilities.ViewCollaborationDashboard,
                Name = "View Collaboration Dashboard",
                Description = "Access the team collaboration dashboard and statistics",
                Category = CapabilityCategories.Collaboration,
                IsEnabled = true
            },
            [LabCapabilities.ViewActivityFeed] = new()
            {
                Id = LabCapabilities.ViewActivityFeed,
                Name = "View Activity Feed",
                Description = "View team activity feed and recent changes",
                Category = CapabilityCategories.Collaboration,
                IsEnabled = true
            },
            [LabCapabilities.ViewTeamPresence] = new()
            {
                Id = LabCapabilities.ViewTeamPresence,
                Name = "View Team Presence",
                Description = "See who is online and active",
                Category = CapabilityCategories.Collaboration,
                IsEnabled = true
            },
            [LabCapabilities.JoinLiveSession] = new()
            {
                Id = LabCapabilities.JoinLiveSession,
                Name = "Join Live Session",
                Description = "Participate in live editing sessions",
                Category = CapabilityCategories.Collaboration,
                IsEnabled = true
            },

            // ─── LIVE EDITING ───
            [LabCapabilities.EditLive] = new()
            {
                Id = LabCapabilities.EditLive,
                Name = "Live Edit",
                Description = "Edit documents and code in real-time",
                Category = CapabilityCategories.LiveEditing,
                IsEnabled = true
            },
            [LabCapabilities.ViewOtherCursors] = new()
            {
                Id = LabCapabilities.ViewOtherCursors,
                Name = "View Other Cursors",
                Description = "See real-time cursor positions from other users",
                Category = CapabilityCategories.LiveEditing,
                IsEnabled = true
            },
            [LabCapabilities.ViewConflictResolution] = new()
            {
                Id = LabCapabilities.ViewConflictResolution,
                Name = "View Conflicts",
                Description = "View detected conflicts in edits",
                Category = CapabilityCategories.LiveEditing,
                IsEnabled = true
            },
            [LabCapabilities.ResolveConflicts] = new()
            {
                Id = LabCapabilities.ResolveConflicts,
                Name = "Resolve Conflicts",
                Description = "Resolve editing conflicts between users",
                Category = CapabilityCategories.LiveEditing,
                IsEnabled = true
            },

            // ─── COMMENTING ───
            [LabCapabilities.ViewComments] = new()
            {
                Id = LabCapabilities.ViewComments,
                Name = "View Comments",
                Description = "View comments on resources and code",
                Category = CapabilityCategories.Commenting,
                IsEnabled = true
            },
            [LabCapabilities.AddComments] = new()
            {
                Id = LabCapabilities.AddComments,
                Name = "Add Comments",
                Description = "Add comments to resources and code",
                Category = CapabilityCategories.Commenting,
                IsEnabled = true
            },
            [LabCapabilities.ReplyToComments] = new()
            {
                Id = LabCapabilities.ReplyToComments,
                Name = "Reply to Comments",
                Description = "Reply to existing comments",
                Category = CapabilityCategories.Commenting,
                IsEnabled = true
            },
            [LabCapabilities.ResolveComments] = new()
            {
                Id = LabCapabilities.ResolveComments,
                Name = "Resolve Comments",
                Description = "Mark comments as resolved",
                Category = CapabilityCategories.Commenting,
                IsEnabled = true
            },
            [LabCapabilities.MentionUsers] = new()
            {
                Id = LabCapabilities.MentionUsers,
                Name = "Mention Users",
                Description = "Mention other users in comments",
                Category = CapabilityCategories.Commenting,
                IsEnabled = true
            },
            [LabCapabilities.ManageComments] = new()
            {
                Id = LabCapabilities.ManageComments,
                Name = "Manage Comments",
                Description = "Manage and delete comments",
                Category = CapabilityCategories.Commenting,
                IsEnabled = true
            },

            // ─── EXPERIMENTATION ───
            [LabCapabilities.ViewExperiments] = new()
            {
                Id = LabCapabilities.ViewExperiments,
                Name = "View Experiments",
                Description = "View experiments and results",
                Category = CapabilityCategories.Experimentation,
                IsEnabled = true
            },
            [LabCapabilities.RunExperiments] = new()
            {
                Id = LabCapabilities.RunExperiments,
                Name = "Run Experiments",
                Description = "Execute experiments",
                Category = CapabilityCategories.Experimentation,
                IsEnabled = true
            },
            [LabCapabilities.CreateExperiments] = new()
            {
                Id = LabCapabilities.CreateExperiments,
                Name = "Create Experiments",
                Description = "Create new experiments",
                Category = CapabilityCategories.Experimentation,
                IsEnabled = true
            },
            [LabCapabilities.CompareExperiments] = new()
            {
                Id = LabCapabilities.CompareExperiments,
                Name = "Compare Experiments",
                Description = "Compare results between experiments",
                Category = CapabilityCategories.Experimentation,
                IsEnabled = true
            },
            [LabCapabilities.ManageExperiments] = new()
            {
                Id = LabCapabilities.ManageExperiments,
                Name = "Manage Experiments",
                Description = "Manage experiments and results",
                Category = CapabilityCategories.Experimentation,
                IsEnabled = true
            },

            // ─── DATA MANAGEMENT ───
            [LabCapabilities.ViewDatasets] = new()
            {
                Id = LabCapabilities.ViewDatasets,
                Name = "View Datasets",
                Description = "View available datasets",
                Category = CapabilityCategories.DataManagement,
                IsEnabled = true
            },
            [LabCapabilities.ImportDatasets] = new()
            {
                Id = LabCapabilities.ImportDatasets,
                Name = "Import Datasets",
                Description = "Import new datasets",
                Category = CapabilityCategories.DataManagement,
                IsEnabled = true
            },
            [LabCapabilities.ExportDatasets] = new()
            {
                Id = LabCapabilities.ExportDatasets,
                Name = "Export Datasets",
                Description = "Export datasets to various formats",
                Category = CapabilityCategories.DataManagement,
                IsEnabled = true
            },
            [LabCapabilities.ProfileData] = new()
            {
                Id = LabCapabilities.ProfileData,
                Name = "Profile Data",
                Description = "Profile and analyze data quality",
                Category = CapabilityCategories.DataManagement,
                IsEnabled = true
            },
            [LabCapabilities.ManageDatasets] = new()
            {
                Id = LabCapabilities.ManageDatasets,
                Name = "Manage Datasets",
                Description = "Manage datasets and permissions",
                Category = CapabilityCategories.DataManagement,
                IsEnabled = true
            },

            // ─── PIPELINE MANAGEMENT ───
            [LabCapabilities.ViewPipelines] = new()
            {
                Id = LabCapabilities.ViewPipelines,
                Name = "View Pipelines",
                Description = "View data pipelines",
                Category = CapabilityCategories.PipelineManagement,
                IsEnabled = true
            },
            [LabCapabilities.ExecutePipelines] = new()
            {
                Id = LabCapabilities.ExecutePipelines,
                Name = "Execute Pipelines",
                Description = "Run data pipelines",
                Category = CapabilityCategories.PipelineManagement,
                IsEnabled = true
            },
            [LabCapabilities.CreatePipelines] = new()
            {
                Id = LabCapabilities.CreatePipelines,
                Name = "Create Pipelines",
                Description = "Create new data pipelines",
                Category = CapabilityCategories.PipelineManagement,
                IsEnabled = true
            },
            [LabCapabilities.EditPipelines] = new()
            {
                Id = LabCapabilities.EditPipelines,
                Name = "Edit Pipelines",
                Description = "Modify pipeline configurations",
                Category = CapabilityCategories.PipelineManagement,
                IsEnabled = true
            },
            [LabCapabilities.ManagePipelines] = new()
            {
                Id = LabCapabilities.ManagePipelines,
                Name = "Manage Pipelines",
                Description = "Manage pipelines and access control",
                Category = CapabilityCategories.PipelineManagement,
                IsEnabled = true
            },
            [LabCapabilities.ConfigureIntegrations] = new()
            {
                Id = LabCapabilities.ConfigureIntegrations,
                Name = "Configure Integrations",
                Description = "Configure data source integrations",
                Category = CapabilityCategories.PipelineManagement,
                IsEnabled = true
            },
            [LabCapabilities.MonitorPipelines] = new()
            {
                Id = LabCapabilities.MonitorPipelines,
                Name = "Monitor Pipelines",
                Description = "Monitor pipeline execution and health",
                Category = CapabilityCategories.PipelineManagement,
                IsEnabled = true
            },

            // ─── MODEL MANAGEMENT ───
            [LabCapabilities.ViewModels] = new()
            {
                Id = LabCapabilities.ViewModels,
                Name = "View Models",
                Description = "View trained models",
                Category = CapabilityCategories.ModelManagement,
                IsEnabled = true
            },
            [LabCapabilities.TrainModels] = new()
            {
                Id = LabCapabilities.TrainModels,
                Name = "Train Models",
                Description = "Train machine learning models",
                Category = CapabilityCategories.ModelManagement,
                IsEnabled = true
            },
            [LabCapabilities.CreateModels] = new()
            {
                Id = LabCapabilities.CreateModels,
                Name = "Create Models",
                Description = "Create new model definitions",
                Category = CapabilityCategories.ModelManagement,
                IsEnabled = true
            },
            [LabCapabilities.EvaluateModels] = new()
            {
                Id = LabCapabilities.EvaluateModels,
                Name = "Evaluate Models",
                Description = "Evaluate model performance",
                Category = CapabilityCategories.ModelManagement,
                IsEnabled = true
            },
            [LabCapabilities.VersionModels] = new()
            {
                Id = LabCapabilities.VersionModels,
                Name = "Version Models",
                Description = "Version and track model iterations",
                Category = CapabilityCategories.ModelManagement,
                IsEnabled = true
            },
            [LabCapabilities.ManageModels] = new()
            {
                Id = LabCapabilities.ManageModels,
                Name = "Manage Models",
                Description = "Manage models and permissions",
                Category = CapabilityCategories.ModelManagement,
                IsEnabled = true
            },

            // ─── DEPLOYMENT ───
            [LabCapabilities.ViewDeployments] = new()
            {
                Id = LabCapabilities.ViewDeployments,
                Name = "View Deployments",
                Description = "View model deployments",
                Category = CapabilityCategories.Deployment,
                IsEnabled = true
            },
            [LabCapabilities.DeployModels] = new()
            {
                Id = LabCapabilities.DeployModels,
                Name = "Deploy Models",
                Description = "Deploy models to production",
                Category = CapabilityCategories.Deployment,
                IsEnabled = true
            },
            [LabCapabilities.ConfigureDeployments] = new()
            {
                Id = LabCapabilities.ConfigureDeployments,
                Name = "Configure Deployments",
                Description = "Configure deployment settings",
                Category = CapabilityCategories.Deployment,
                IsEnabled = true
            },
            [LabCapabilities.ManageDeployments] = new()
            {
                Id = LabCapabilities.ManageDeployments,
                Name = "Manage Deployments",
                Description = "Manage deployments and permissions",
                Category = CapabilityCategories.Deployment,
                IsEnabled = true
            },
            [LabCapabilities.OptimizeDeployments] = new()
            {
                Id = LabCapabilities.OptimizeDeployments,
                Name = "Optimize Deployments",
                Description = "Optimize deployment performance",
                Category = CapabilityCategories.Deployment,
                IsEnabled = true
            },
            [LabCapabilities.ManageAPIs] = new()
            {
                Id = LabCapabilities.ManageAPIs,
                Name = "Manage APIs",
                Description = "Manage model serving APIs",
                Category = CapabilityCategories.Deployment,
                IsEnabled = true
            },

            // ─── MONITORING ───
            [LabCapabilities.ViewMetrics] = new()
            {
                Id = LabCapabilities.ViewMetrics,
                Name = "View Metrics",
                Description = "View system and model metrics",
                Category = CapabilityCategories.Monitoring,
                IsEnabled = true
            },
            [LabCapabilities.ViewLogs] = new()
            {
                Id = LabCapabilities.ViewLogs,
                Name = "View Logs",
                Description = "View system logs",
                Category = CapabilityCategories.Monitoring,
                IsEnabled = true
            },
            [LabCapabilities.MonitorPerformance] = new()
            {
                Id = LabCapabilities.MonitorPerformance,
                Name = "Monitor Performance",
                Description = "Monitor system and model performance",
                Category = CapabilityCategories.Monitoring,
                IsEnabled = true
            },
            [LabCapabilities.ManageAlerts] = new()
            {
                Id = LabCapabilities.ManageAlerts,
                Name = "Manage Alerts",
                Description = "Manage monitoring alerts and thresholds",
                Category = CapabilityCategories.Monitoring,
                IsEnabled = true
            }
        };
    }

    /// <summary>
    /// Initialize role-to-capability mappings.
    /// Distributes capabilities across 4 professional roles.
    /// </summary>
    private Dictionary<RoleType, HashSet<string>> InitializeRoleCapabilities()
    {
        return new Dictionary<RoleType, HashSet<string>>
        {
            // ─── DATA ANALYST ───
            [RoleType.DataAnalyst] = new()
            {
                // Collaboration
                LabCapabilities.ViewCollaborationDashboard,
                LabCapabilities.ViewActivityFeed,
                LabCapabilities.ViewTeamPresence,
                LabCapabilities.JoinLiveSession,

                // Commenting
                LabCapabilities.ViewComments,
                LabCapabilities.AddComments,
                LabCapabilities.ReplyToComments,
                LabCapabilities.MentionUsers,

                // Data
                LabCapabilities.ViewDatasets,
                LabCapabilities.ExportDatasets,
                LabCapabilities.ProfileData,

                // Viewing
                LabCapabilities.ViewExperiments,
                LabCapabilities.ViewModels,
                LabCapabilities.ViewPipelines,
                LabCapabilities.ViewDeployments,
                LabCapabilities.ViewMetrics,
            },

            // ─── DATA SCIENTIST ───
            [RoleType.DataScientist] = new()
            {
                // All Analyst capabilities
                LabCapabilities.ViewCollaborationDashboard,
                LabCapabilities.ViewActivityFeed,
                LabCapabilities.ViewTeamPresence,
                LabCapabilities.JoinLiveSession,
                LabCapabilities.ViewComments,
                LabCapabilities.AddComments,
                LabCapabilities.ReplyToComments,
                LabCapabilities.MentionUsers,
                LabCapabilities.ViewDatasets,
                LabCapabilities.ExportDatasets,
                LabCapabilities.ProfileData,
                LabCapabilities.ViewExperiments,
                LabCapabilities.ViewModels,
                LabCapabilities.ViewPipelines,
                LabCapabilities.ViewDeployments,
                LabCapabilities.ViewMetrics,

                // Live Editing & Collaboration (Extended)
                LabCapabilities.EditLive,
                LabCapabilities.ViewOtherCursors,
                LabCapabilities.ViewConflictResolution,
                LabCapabilities.ResolveConflicts,
                LabCapabilities.ResolveComments,

                // Experimentation
                LabCapabilities.RunExperiments,
                LabCapabilities.CreateExperiments,
                LabCapabilities.CompareExperiments,
                LabCapabilities.ManageExperiments,

                // Data
                LabCapabilities.ImportDatasets,
                LabCapabilities.ManageDatasets,

                // Models
                LabCapabilities.TrainModels,
                LabCapabilities.CreateModels,
                LabCapabilities.EvaluateModels,
                LabCapabilities.VersionModels,

                // Deployment
                LabCapabilities.ViewDeployments,
                LabCapabilities.DeployModels,
            },

            // ─── DATA ENGINEER ───
            [RoleType.DataEngineer] = new()
            {
                // Collaboration & Communication
                LabCapabilities.ViewCollaborationDashboard,
                LabCapabilities.ViewActivityFeed,
                LabCapabilities.ViewTeamPresence,
                LabCapabilities.JoinLiveSession,
                LabCapabilities.ViewComments,
                LabCapabilities.AddComments,
                LabCapabilities.ReplyToComments,
                LabCapabilities.MentionUsers,
                LabCapabilities.ResolveComments,

                // Live Editing
                LabCapabilities.EditLive,
                LabCapabilities.ViewOtherCursors,
                LabCapabilities.ViewConflictResolution,
                LabCapabilities.ResolveConflicts,

                // Data (Full Control)
                LabCapabilities.ViewDatasets,
                LabCapabilities.ImportDatasets,
                LabCapabilities.ExportDatasets,
                LabCapabilities.ProfileData,
                LabCapabilities.ManageDatasets,

                // Pipelines (Full Control)
                LabCapabilities.ViewPipelines,
                LabCapabilities.ExecutePipelines,
                LabCapabilities.CreatePipelines,
                LabCapabilities.EditPipelines,
                LabCapabilities.ManagePipelines,
                LabCapabilities.ConfigureIntegrations,
                LabCapabilities.MonitorPipelines,

                // Experimentation
                LabCapabilities.ViewExperiments,
                LabCapabilities.RunExperiments,

                // Models (View & Monitor)
                LabCapabilities.ViewModels,
                LabCapabilities.ViewDeployments,

                // Monitoring
                LabCapabilities.ViewMetrics,
                LabCapabilities.ViewLogs,
                LabCapabilities.MonitorPerformance,
            },

            // ─── AI ENGINEER ───
            [RoleType.AIEngineer] = new()
            {
                // Collaboration (Full)
                LabCapabilities.ViewCollaborationDashboard,
                LabCapabilities.ViewActivityFeed,
                LabCapabilities.ViewTeamPresence,
                LabCapabilities.JoinLiveSession,
                LabCapabilities.ViewComments,
                LabCapabilities.AddComments,
                LabCapabilities.ReplyToComments,
                LabCapabilities.MentionUsers,
                LabCapabilities.ResolveComments,
                LabCapabilities.ManageComments,

                // Live Editing (Full)
                LabCapabilities.EditLive,
                LabCapabilities.ViewOtherCursors,
                LabCapabilities.ViewConflictResolution,
                LabCapabilities.ResolveConflicts,

                // Data (View & Access)
                LabCapabilities.ViewDatasets,
                LabCapabilities.ExportDatasets,
                LabCapabilities.ProfileData,

                // Models (Full Control)
                LabCapabilities.ViewModels,
                LabCapabilities.TrainModels,
                LabCapabilities.CreateModels,
                LabCapabilities.EvaluateModels,
                LabCapabilities.VersionModels,
                LabCapabilities.ManageModels,

                // Deployment (Full Control)
                LabCapabilities.ViewDeployments,
                LabCapabilities.DeployModels,
                LabCapabilities.ConfigureDeployments,
                LabCapabilities.ManageDeployments,
                LabCapabilities.OptimizeDeployments,
                LabCapabilities.ManageAPIs,

                // Monitoring (Full)
                LabCapabilities.ViewMetrics,
                LabCapabilities.ViewLogs,
                LabCapabilities.MonitorPerformance,
                LabCapabilities.ManageAlerts,

                // Experimentation
                LabCapabilities.ViewExperiments,
                LabCapabilities.RunExperiments,
                LabCapabilities.CompareExperiments,
            }
        };
    }
}

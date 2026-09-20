using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.Projects;
using AimranDataScienceLab.Core.Domain.RoleManagement;
using AIMRAN_Data_Science_Lab.Domain.Collaboration;

namespace AimranDataScienceLab.Shared.Services.Projects;

/// <summary>
/// Service for managing unified data science projects.
/// All 4 roles work within the same project context with shared resources.
/// </summary>
public interface IProjectService
{
    // Project Management
    Task<DataScienceProject> CreateProjectAsync(DataScienceProject project);
    Task<DataScienceProject?> GetProjectAsync(Guid projectId);
    Task<DataScienceProject[]> GetUserProjectsAsync(Guid userId);
    Task UpdateProjectAsync(DataScienceProject project);
    Task DeleteProjectAsync(Guid projectId);
    Task ArchiveProjectAsync(Guid projectId);

    // Team Management
    Task AddTeamMemberAsync(Guid projectId, Guid userId, string role);
    Task RemoveTeamMemberAsync(Guid projectId, Guid userId);
    Task UpdateTeamMemberRoleAsync(Guid projectId, Guid userId, string role);
    Task<(Guid UserId, string Role)[]> GetTeamMembersAsync(Guid projectId);

    // Workflow Management
    Task<ProjectWorkflow> CreateWorkflowAsync(ProjectWorkflow workflow);
    Task<ProjectWorkflow?> GetWorkflowAsync(Guid workflowId);
    Task<ProjectWorkflow[]> GetProjectWorkflowsAsync(Guid projectId);
    Task UpdateWorkflowAsync(ProjectWorkflow workflow);
    Task CompleteWorkflowStageAsync(Guid workflowId, string stageName);

    // Phase Management
    Task UpdateProjectPhaseAsync(Guid projectId, ProjectPhase newPhase);
    Task<ProjectPhase> GetProjectPhaseAsync(Guid projectId);

    // Resource Association
    Task AssociateDatasetAsync(Guid projectId, Guid datasetId);
    Task AssociateModelAsync(Guid projectId, Guid modelId);
    Task AssociatePipelineAsync(Guid projectId, Guid pipelineId);

    // Cross-role Visibility
    Task<ProjectContext> GetProjectContextAsync(Guid projectId, Guid userId);

    // ===== REAL-TIME COLLABORATION =====

    /// <summary>
    /// Get recent team collaboration activity in project.
    /// </summary>
    Task<TeamActivitySummary> GetProjectActivitySummaryAsync(Guid projectId);

    /// <summary>
    /// Get all users currently active in project.
    /// </summary>
    Task<List<UserPresence>> GetProjectParticipantsAsync(Guid projectId);

    /// <summary>
    /// Log project activity event.
    /// </summary>
    Task LogProjectActivityAsync(Guid projectId, string userId, string activityType, string description);

    /// <summary>
    /// Get collaboration comments for a resource in project.
    /// </summary>
    Task<List<CollaborativeComment>> GetResourceCommentsAsync(Guid projectId, string resourceId);

    /// <summary>
    /// Get team activity session for project.
    /// </summary>
    Task<TeamActivitySession> GetTeamSessionAsync(Guid projectId);
}

/// <summary>
/// Comprehensive project context for a specific user.
/// Shows only resources and workflows relevant to their role.
/// </summary>
public class ProjectContext
{
    public DataScienceProject Project { get; set; } = new();
    public ProfessionalRole UserRole { get; set; } = new();

    // Resources visible to this role
    public Guid[] VisibleDatasets { get; set; } = Array.Empty<Guid>();
    public Guid[] VisibleModels { get; set; } = Array.Empty<Guid>();
    public Guid[] VisiblePipelines { get; set; } = Array.Empty<Guid>();

    // Workflows assigned to this role
    public ProjectWorkflow[] AssignedWorkflows { get; set; } = Array.Empty<ProjectWorkflow>();

    // Cross-role dependencies
    public WorkflowDependency[] WorkflowDependencies { get; set; } = Array.Empty<WorkflowDependency>();

    // Available actions for this role
    public AvailableAction[] AvailableActions { get; set; } = Array.Empty<AvailableAction>();

    public DateTime LoadedAt { get; set; }
}

public class AvailableAction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetWorkflow { get; set; } = string.Empty;
}

/// <summary>
/// Service event for cross-role notifications and synchronization.
/// </summary>
public interface IProjectEventService
{
    Task RaiseProjectEventAsync(ProjectEvent projectEvent);
    Task SubscribeToProjectEventsAsync(Guid projectId, Func<ProjectEvent, Task> handler);
}

public class ProjectEvent
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public ProjectEventType Type { get; set; }

    public string ResourceType { get; set; } = string.Empty; // Dataset, Model, Pipeline
    public Guid ResourceId { get; set; }

    public Guid InitiatedByUserId { get; set; }
    public string InitiatedByRole { get; set; } = string.Empty;

    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime OccurredAt { get; set; }
}

public enum ProjectEventType
{
    DatasetAdded,
    DatasetVersioned,
    DatasetCleaned,
    ModelCreated,
    ModelTrained,
    ModelEvaluated,
    ModelApproved,
    PipelineCreated,
    PipelineExecuted,
    WorkflowCompleted,
    InsightGenerated,
    DeploymentInitiated,
    MonitoringAlert,
    PhaseTransition
}

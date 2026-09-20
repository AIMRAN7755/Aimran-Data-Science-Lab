using System;
using System.Collections.Generic;

namespace AimranDataScienceLab.Core.Domain.Projects;

/// <summary>
/// Represents a unified project context shared across all 4 roles.
/// All roles work within the same project with shared datasets and models.
/// </summary>
public class DataScienceProject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public Guid OwnerId { get; set; } // User who created project
    public Guid[] TeamMemberIds { get; set; } = Array.Empty<Guid>();
    public Dictionary<Guid, string> RoleAssignments { get; set; } = new(); // UserId -> Role

    public ProjectObjective Objective { get; set; } = new();
    public ProjectPhase CurrentPhase { get; set; } = ProjectPhase.ProblemDefinition;

    public Guid[] DatasetIds { get; set; } = Array.Empty<Guid>(); // All datasets in project
    public Guid[] ModelIds { get; set; } = Array.Empty<Guid>(); // All models in project
    public Guid[] PipelineIds { get; set; } = Array.Empty<Guid>(); // All pipelines in project

    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Archived
}

public enum ProjectPhase
{
    ProblemDefinition = 1,
    DataCollection = 2,
    DataCleaning = 3,
    EDA = 4,
    FeatureEngineering = 5,
    Modeling = 6,
    Evaluation = 7,
    Visualization = 8,
    Deployment = 9,
    Monitoring = 10
}

/// <summary>
/// Project objective with KPIs and constraints.
/// </summary>
public class ProjectObjective
{
    public string Goal { get; set; } = string.Empty;
    public string[] KPIs { get; set; } = Array.Empty<string>();
    public string[] BusinessConstraints { get; set; } = Array.Empty<string>();
    public string[] TechnicalConstraints { get; set; } = Array.Empty<string>();
    public string[]? SuccessCriteria { get; set; }
}

/// <summary>
/// Represents a workflow within a project.
/// Workflows can be shared or role-specific.
/// </summary>
public class ProjectWorkflow
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkflowType Type { get; set; }

    public string[] AssignedRoles { get; set; } = Array.Empty<string>();
    public WorkflowStage[] Stages { get; set; } = Array.Empty<WorkflowStage>();
    public WorkflowDependency[] Dependencies { get; set; } = Array.Empty<WorkflowDependency>();

    public WorkflowStatus Status { get; set; } = WorkflowStatus.NotStarted;
    public int ProgressPercent { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum WorkflowType
{
    DataIngestion,
    DataCleaning,
    EDA,
    FeatureEngineering,
    ModelBuilding,
    ModelEvaluation,
    ModelDeployment,
    Monitoring,
    Custom
}

public enum WorkflowStatus
{
    NotStarted,
    InProgress,
    Completed,
    OnHold,
    Failed
}

public class WorkflowStage
{
    public string Name { get; set; } = string.Empty;
    public string AssignedRole { get; set; } = string.Empty;
    public int Order { get; set; }
    public string[] InputResources { get; set; } = Array.Empty<string>();
    public string[] OutputResources { get; set; } = Array.Empty<string>();
    public TimeSpan? EstimatedDuration { get; set; }
    public bool IsCompleted { get; set; }
}

public class WorkflowDependency
{
    public Guid SourceWorkflowId { get; set; }
    public Guid TargetWorkflowId { get; set; }
    public DependencyType Type { get; set; }
}

public enum DependencyType
{
    Sequential,      // Must complete before next starts
    DataDependency,  // Requires output data from source
    Parallel         // Can run simultaneously
}

namespace AIMRAN_Data_Science_Lab.Models.DataCleaning;

/// <summary>
/// A reusable cleaning rule that can be learned and applied.
/// </summary>
public record CleaningRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public CleaningRuleType Type { get; init; }
    public CleaningRuleScope Scope { get; init; }
    public IReadOnlyList<RuleCondition> Conditions { get; init; } = [];
    public IReadOnlyList<RuleAction> Actions { get; init; } = [];
    public double Priority { get; init; } = 1.0;
    public bool IsEnabled { get; init; } = true;
    public bool IsLearned { get; init; }
    public int TimesApplied { get; init; }
    public double SuccessRate { get; init; }
    public string? CreatedByUserId { get; init; }
    public string? ProjectId { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastAppliedAt { get; init; }
}

/// <summary>
/// Condition that must be met for a rule to apply.
/// </summary>
public record RuleCondition
{
    public RuleConditionType Type { get; init; }
    public string? ColumnName { get; init; }
    public string? ColumnPattern { get; init; }
    public ColumnDataType? DataType { get; init; }
    public ComparisonOperator Operator { get; init; }
    public object? Value { get; init; }
    public double? Threshold { get; init; }
}

/// <summary>
/// Action to take when rule conditions are met.
/// </summary>
public record RuleAction
{
    public CleaningOperationType OperationType { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public int Order { get; init; }
}

/// <summary>
/// A complete cleaning pipeline composed of multiple operations.
/// </summary>
public record CleaningPipeline
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public Guid? ParentPipelineId { get; init; }
    public PipelineCategory Category { get; init; }
    public IReadOnlyList<PipelineStep> Steps { get; init; } = [];
    public IReadOnlyDictionary<string, object> DefaultParameters { get; init; } = new Dictionary<string, object>();
    public CleaningAggressiveness Aggressiveness { get; init; } = CleaningAggressiveness.Balanced;
    public bool IsTemplate { get; init; }
    public bool IsPublic { get; init; }
    public string? Industry { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public int TimesUsed { get; init; }
    public double AverageQualityImprovement { get; init; }
    public string? CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// A single step in a cleaning pipeline.
/// </summary>
public record PipelineStep
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int Order { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public CleaningOperationType OperationType { get; init; }
    public IReadOnlyList<string>? TargetColumns { get; init; }
    public string? ColumnPattern { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public IReadOnlyList<RuleCondition> Preconditions { get; init; } = [];
    public bool IsEnabled { get; init; } = true;
    public bool ContinueOnError { get; init; }
}

/// <summary>
/// Execution result of a pipeline.
/// </summary>
public record PipelineExecutionResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PipelineId { get; init; }
    public Guid DatasetId { get; init; }
    public PipelineExecutionStatus Status { get; init; }
    public IReadOnlyList<PipelineStepResult> StepResults { get; init; } = [];
    public int TotalSteps { get; init; }
    public int CompletedSteps { get; init; }
    public int FailedSteps { get; init; }
    public int SkippedSteps { get; init; }
    public CleaningImpactMetrics? OverallImpact { get; init; }
    public string? OutputFilePath { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of executing a single pipeline step.
/// </summary>
public record PipelineStepResult
{
    public Guid StepId { get; init; }
    public int Order { get; init; }
    public string StepName { get; init; } = string.Empty;
    public PipelineStepStatus Status { get; init; }
    public CleaningOperationResult? OperationResult { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SkipReason { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// History entry for cleaning version control.
/// </summary>
public record CleaningHistoryEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DatasetId { get; init; }
    public Guid? SessionId { get; init; }
    public int Version { get; init; }
    public CleaningHistoryAction Action { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    public string? SnapshotPath { get; init; }
    public string? DiffPath { get; init; }
    public DataQualityScore? QualityScoreSnapshot { get; init; }
    public string? UserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// User action for self-learning system.
/// </summary>
public record UserCleaningAction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserId { get; init; } = string.Empty;
    public string? ProjectId { get; init; }
    public Guid DatasetId { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public ColumnDataType ColumnDataType { get; init; }
    public CleaningOperationType OperationType { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public bool WasSuccessful { get; init; }
    public double? QualityImprovement { get; init; }
    public DateTime PerformedAt { get; init; } = DateTime.UtcNow;
}

public enum CleaningRuleType
{
    ColumnBased,
    RowBased,
    ValueBased,
    PatternBased,
    Statistical,
    DomainSpecific
}

public enum CleaningRuleScope
{
    Global,
    Project,
    Dataset,
    User
}

public enum RuleConditionType
{
    ColumnName,
    ColumnNamePattern,
    DataType,
    MissingPercentage,
    UniquePercentage,
    OutlierPercentage,
    ValueRange,
    ValuePattern,
    Custom
}

public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    Matches,
    In,
    NotIn
}

public enum PipelineCategory
{
    General,
    Finance,
    Healthcare,
    Ecommerce,
    IoT,
    Marketing,
    Scientific,
    Custom
}

public enum CleaningAggressiveness
{
    Conservative,
    Balanced,
    Aggressive,
    Custom
}

public enum PipelineExecutionStatus
{
    Pending,
    Running,
    Completed,
    CompletedWithErrors,
    Failed,
    Cancelled
}

public enum PipelineStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

public enum CleaningHistoryAction
{
    Created,
    ProfileGenerated,
    CleaningApplied,
    PipelineExecuted,
    Reverted,
    Exported,
    Snapshot
}

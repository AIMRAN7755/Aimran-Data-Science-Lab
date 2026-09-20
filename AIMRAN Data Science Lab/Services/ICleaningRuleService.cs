using AIMRAN_Data_Science_Lab.Models.DataCleaning;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing cleaning rules, pipelines, and self-learning system.
/// </summary>
public interface ICleaningRuleService
{
    #region Rule Management

    /// <summary>
    /// Create a new cleaning rule.
    /// </summary>
    Task<CleaningRule> CreateRuleAsync(
        CleaningRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all rules, optionally filtered by scope.
    /// </summary>
    Task<IReadOnlyList<CleaningRule>> GetRulesAsync(
        CleaningRuleScope? scope = null,
        string? projectId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific rule by ID.
    /// </summary>
    Task<CleaningRule?> GetRuleByIdAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing rule.
    /// </summary>
    Task<CleaningRule> UpdateRuleAsync(
        CleaningRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a rule.
    /// </summary>
    Task DeleteRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find rules applicable to a dataset based on its profile.
    /// </summary>
    Task<IReadOnlyList<CleaningRule>> FindApplicableRulesAsync(
        Guid datasetId,
        DataProfile? profile = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Pipeline Management

    /// <summary>
    /// Create a new cleaning pipeline.
    /// </summary>
    Task<CleaningPipeline> CreatePipelineAsync(
        CleaningPipeline pipeline,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pipelines, optionally filtered by category.
    /// </summary>
    Task<IReadOnlyList<CleaningPipeline>> GetPipelinesAsync(
        PipelineCategory? category = null,
        bool includeTemplates = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific pipeline by ID.
    /// </summary>
    Task<CleaningPipeline?> GetPipelineByIdAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing pipeline.
    /// </summary>
    Task<CleaningPipeline> UpdatePipelineAsync(
        CleaningPipeline pipeline,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a pipeline.
    /// </summary>
    Task DeletePipelineAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new version of an existing pipeline.
    /// </summary>
    Task<CleaningPipeline> CreatePipelineVersionAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pipeline templates for a specific industry.
    /// </summary>
    Task<IReadOnlyList<CleaningPipeline>> GetIndustryTemplatesAsync(
        string industry,
        CancellationToken cancellationToken = default);

    #endregion

    #region Self-Learning System

    /// <summary>
    /// Record a user's cleaning action for learning.
    /// </summary>
    Task RecordUserActionAsync(
        UserCleaningAction action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get learned rules based on user actions.
    /// </summary>
    Task<IReadOnlyList<CleaningRule>> GetLearnedRulesAsync(
        string? userId = null,
        string? projectId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggest cleaning actions based on learned patterns.
    /// </summary>
    Task<IReadOnlyList<LearnedSuggestion>> GetLearnedSuggestionsAsync(
        Guid datasetId,
        DataProfile? profile = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Train the learning model with historical actions.
    /// </summary>
    Task TrainLearningModelAsync(
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get learning statistics.
    /// </summary>
    Task<LearningStatistics> GetLearningStatisticsAsync(
        string? userId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Version Control

    /// <summary>
    /// Get cleaning history for a dataset.
    /// </summary>
    Task<IReadOnlyList<CleaningHistoryEntry>> GetCleaningHistoryAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a snapshot of the current dataset state.
    /// </summary>
    Task<CleaningHistoryEntry> CreateSnapshotAsync(
        Guid datasetId,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get diff between two versions.
    /// </summary>
    Task<CleaningDiff> GetDiffAsync(
        Guid datasetId,
        int version1,
        int version2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback to a specific version.
    /// </summary>
    Task<CleaningHistoryEntry> RollbackToVersionAsync(
        Guid datasetId,
        int targetVersion,
        CancellationToken cancellationToken = default);

    #endregion

    #region Templates

    /// <summary>
    /// Get built-in cleaning templates.
    /// </summary>
    Task<IReadOnlyList<CleaningPipeline>> GetBuiltInTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a template from an existing session.
    /// </summary>
    Task<CleaningPipeline> CreateTemplateFromSessionAsync(
        Guid sessionId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import a pipeline template.
    /// </summary>
    Task<CleaningPipeline> ImportTemplateAsync(
        string templateJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export a pipeline as template.
    /// </summary>
    Task<string> ExportTemplateAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// A suggestion learned from user behavior.
/// </summary>
public record LearnedSuggestion
{
    public string ColumnName { get; init; } = string.Empty;
    public CleaningOperationType SuggestedOperation { get; init; }
    public IReadOnlyDictionary<string, object> SuggestedParameters { get; init; } = new Dictionary<string, object>();
    public double ConfidenceScore { get; init; }
    public string Rationale { get; init; } = string.Empty;
    public int TimesPreviouslyUsed { get; init; }
    public double PreviousSuccessRate { get; init; }
}

/// <summary>
/// Statistics about the learning system.
/// </summary>
public record LearningStatistics
{
    public int TotalActionsRecorded { get; init; }
    public int RulesLearned { get; init; }
    public int PatternsDetected { get; init; }
    public double AverageSuggestionAccuracy { get; init; }
    public IReadOnlyDictionary<CleaningOperationType, int> OperationFrequency { get; init; } = new Dictionary<CleaningOperationType, int>();
    public IReadOnlyList<PopularPattern> PopularPatterns { get; init; } = [];
    public DateTime LastTrainingDate { get; init; }
}

/// <summary>
/// A commonly observed cleaning pattern.
/// </summary>
public record PopularPattern
{
    public string Description { get; init; } = string.Empty;
    public ColumnDataType ApplicableColumnType { get; init; }
    public CleaningOperationType Operation { get; init; }
    public int TimesObserved { get; init; }
    public double SuccessRate { get; init; }
}

/// <summary>
/// Difference between two dataset versions.
/// </summary>
public record CleaningDiff
{
    public Guid DatasetId { get; init; }
    public int FromVersion { get; init; }
    public int ToVersion { get; init; }
    public int RowsAdded { get; init; }
    public int RowsRemoved { get; init; }
    public int RowsModified { get; init; }
    public int CellsModified { get; init; }
    public IReadOnlyList<ColumnDiff> ColumnDiffs { get; init; } = [];
    public IReadOnlyList<string> ColumnsAdded { get; init; } = [];
    public IReadOnlyList<string> ColumnsRemoved { get; init; } = [];
    public DataQualityScore? QualityScoreBefore { get; init; }
    public DataQualityScore? QualityScoreAfter { get; init; }
}

/// <summary>
/// Difference in a single column between versions.
/// </summary>
public record ColumnDiff
{
    public string ColumnName { get; init; } = string.Empty;
    public int ValuesAdded { get; init; }
    public int ValuesRemoved { get; init; }
    public int ValuesModified { get; init; }
    public IReadOnlyList<ValueChange> SampleChanges { get; init; } = [];
}

/// <summary>
/// A single value change.
/// </summary>
public record ValueChange
{
    public int RowIndex { get; init; }
    public string OldValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
}

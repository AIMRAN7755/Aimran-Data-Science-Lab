using AIMRAN_Data_Science_Lab.Models.DataCleaning;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Main service for data cleaning operations.
/// Provides imputation, normalization, and pipeline execution capabilities.
/// </summary>
public interface IDataCleaningService
{
    #region Imputation Operations

    /// <summary>
    /// Impute missing values in specified columns using the given strategy.
    /// </summary>
    Task<IReadOnlyList<ImputationResult>> ImputeMissingValuesAsync(
        Guid datasetId,
        IEnumerable<string> columns,
        ImputationStrategy strategy,
        IReadOnlyDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI-recommended imputation strategy for each column.
    /// </summary>
    Task<IReadOnlyDictionary<string, ImputationRecommendation>> GetImputationRecommendationsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drop rows with missing values above threshold.
    /// </summary>
    Task<CleaningOperationResult> DropMissingRowsAsync(
        Guid datasetId,
        double threshold = 0.5,
        IEnumerable<string>? columns = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drop columns with missing values above threshold.
    /// </summary>
    Task<CleaningOperationResult> DropMissingColumnsAsync(
        Guid datasetId,
        double threshold = 0.5,
        CancellationToken cancellationToken = default);

    #endregion

    #region Format Normalization

    /// <summary>
    /// Normalize date/time formats in specified columns.
    /// </summary>
    Task<NormalizationResult> NormalizeDateTimeAsync(
        Guid datasetId,
        string column,
        string targetFormat = "yyyy-MM-dd HH:mm:ss",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalize currency values in specified columns.
    /// </summary>
    Task<NormalizationResult> NormalizeCurrencyAsync(
        Guid datasetId,
        string column,
        string targetCurrency = "USD",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalize text encoding in specified columns.
    /// </summary>
    Task<NormalizationResult> NormalizeEncodingAsync(
        Guid datasetId,
        string column,
        string targetEncoding = "UTF-8",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-detect and normalize regional formats.
    /// </summary>
    Task<IReadOnlyList<NormalizationResult>> AutoNormalizeFormatsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Outlier Handling

    /// <summary>
    /// Remove detected outliers from dataset.
    /// </summary>
    Task<CleaningOperationResult> RemoveOutliersAsync(
        Guid datasetId,
        OutlierDetectionResult detectionResult,
        OutlierSeverity minimumSeverity = OutlierSeverity.Moderate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap outliers to boundary values instead of removing.
    /// </summary>
    Task<CleaningOperationResult> CapOutliersAsync(
        Guid datasetId,
        string column,
        double? lowerBound = null,
        double? upperBound = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transform outliers using specified method.
    /// </summary>
    Task<CleaningOperationResult> TransformOutliersAsync(
        Guid datasetId,
        string column,
        OutlierTransformMethod method,
        CancellationToken cancellationToken = default);

    #endregion

    #region Text Cleaning

    /// <summary>
    /// Clean text columns by removing stopwords, trimming, etc.
    /// </summary>
    Task<CleaningOperationResult> CleanTextAsync(
        Guid datasetId,
        string column,
        TextCleaningOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove duplicate rows from dataset.
    /// </summary>
    Task<CleaningOperationResult> RemoveDuplicatesAsync(
        Guid datasetId,
        IEnumerable<string>? columns = null,
        DuplicateKeepStrategy keepStrategy = DuplicateKeepStrategy.First,
        CancellationToken cancellationToken = default);

    #endregion

    #region Pipeline Execution

    /// <summary>
    /// Execute a cleaning pipeline on the dataset.
    /// </summary>
    Task<PipelineExecutionResult> ExecutePipelineAsync(
        Guid datasetId,
        CleaningPipeline pipeline,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute auto-clean with adjustable aggressiveness.
    /// </summary>
    Task<PipelineExecutionResult> AutoCleanAsync(
        Guid datasetId,
        CleaningAggressiveness aggressiveness = CleaningAggressiveness.Balanced,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview cleaning operation results without applying.
    /// </summary>
    Task<CleaningPreview> PreviewCleaningAsync(
        Guid datasetId,
        CleaningOperation operation,
        int sampleSize = 100,
        CancellationToken cancellationToken = default);

    #endregion

    #region Session Management

    /// <summary>
    /// Create a new cleaning session for a dataset.
    /// </summary>
    Task<CleaningSession> CreateSessionAsync(
        Guid datasetId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all cleaning sessions for a dataset.
    /// </summary>
    Task<IReadOnlyList<CleaningSession>> GetSessionsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply a cleaning session and save results.
    /// </summary>
    Task<CleaningSession> ApplySessionAsync(
        Guid sessionId,
        string? outputPath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback to a previous session version.
    /// </summary>
    Task<CleaningSession> RollbackSessionAsync(
        Guid sessionId,
        int targetVersion,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Recommendation for imputation strategy.
/// </summary>
public record ImputationRecommendation
{
    public string ColumnName { get; init; } = string.Empty;
    public ImputationStrategy RecommendedStrategy { get; init; }
    public double ConfidenceScore { get; init; }
    public string Rationale { get; init; } = string.Empty;
    public IReadOnlyList<ImputationStrategy> AlternativeStrategies { get; init; } = [];
}

/// <summary>
/// Options for text cleaning operations.
/// </summary>
public record TextCleaningOptions
{
    public bool RemoveStopwords { get; init; }
    public bool TrimWhitespace { get; init; } = true;
    public bool NormalizeWhitespace { get; init; } = true;
    public bool RemoveSpecialCharacters { get; init; }
    public bool ConvertToLowercase { get; init; }
    public bool ConvertToUppercase { get; init; }
    public bool RemoveNumbers { get; init; }
    public bool RemoveUrls { get; init; }
    public bool RemoveEmails { get; init; }
    public bool CorrectSpelling { get; init; }
    public string? Language { get; init; } = "en";
    public IReadOnlyList<string>? CustomStopwords { get; init; }
}

/// <summary>
/// Preview of cleaning operation results.
/// </summary>
public record CleaningPreview
{
    public int TotalRowsAffected { get; init; }
    public int TotalCellsModified { get; init; }
    public IReadOnlyList<PreviewChange> SampleChanges { get; init; } = [];
    public CleaningImpactMetrics EstimatedImpact { get; init; } = new();
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// A single change in the preview.
/// </summary>
public record PreviewChange
{
    public int RowIndex { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public string OriginalValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
    public string ChangeType { get; init; } = string.Empty;
}

/// <summary>
/// Progress information for pipeline execution.
/// </summary>
public record PipelineProgress
{
    public int CurrentStep { get; init; }
    public int TotalSteps { get; init; }
    public string CurrentStepName { get; init; } = string.Empty;
    public double PercentComplete { get; init; }
    public string Status { get; init; } = string.Empty;
}

public enum OutlierTransformMethod
{
    Log,
    SquareRoot,
    Winsorize,
    Standardize
}

public enum DuplicateKeepStrategy
{
    First,
    Last,
    None
}

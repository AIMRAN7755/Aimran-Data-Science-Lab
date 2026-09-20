namespace AIMRAN_Data_Science_Lab.Models.DataCleaning;

/// <summary>
/// Represents a cleaning operation to be applied to data.
/// </summary>
public record CleaningOperation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public CleaningOperationType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> TargetColumns { get; init; } = [];
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public int Order { get; init; }
    public bool IsEnabled { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Result of applying a cleaning operation.
/// </summary>
public record CleaningOperationResult
{
    public Guid OperationId { get; init; }
    public CleaningOperationType OperationType { get; init; }
    public bool Success { get; init; }
    public int RowsAffected { get; init; }
    public int CellsModified { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyDictionary<string, ColumnCleaningStats> ColumnStats { get; init; } = new Dictionary<string, ColumnCleaningStats>();
    public CleaningImpactMetrics? ImpactMetrics { get; init; }
}

/// <summary>
/// Statistics for cleaning applied to a single column.
/// </summary>
public record ColumnCleaningStats
{
    public string ColumnName { get; init; } = string.Empty;
    public int ValuesModified { get; init; }
    public int ValuesRemoved { get; init; }
    public int ValuesImputed { get; init; }
    public IReadOnlyList<string> SampleChanges { get; init; } = [];
}

/// <summary>
/// Impact metrics showing how cleaning affected data quality.
/// </summary>
public record CleaningImpactMetrics
{
    public double QualityScoreBefore { get; init; }
    public double QualityScoreAfter { get; init; }
    public double QualityImprovement => QualityScoreAfter - QualityScoreBefore;
    public int MissingValuesBefore { get; init; }
    public int MissingValuesAfter { get; init; }
    public int OutliersBefore { get; init; }
    public int OutliersAfter { get; init; }
    public double? EstimatedModelAccuracyImpact { get; init; }
    public double DataLossPercentage { get; init; }
}

/// <summary>
/// Result of missing value imputation.
/// </summary>
public record ImputationResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ColumnName { get; init; } = string.Empty;
    public ImputationStrategy Strategy { get; init; }
    public int ValuesImputed { get; init; }
    public double? ImputedValue { get; init; }
    public double ConfidenceScore { get; init; }
    public IReadOnlyList<ImputationDetail> Details { get; init; } = [];
    public DateTime ImputedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Detail of a single imputation.
/// </summary>
public record ImputationDetail
{
    public int RowIndex { get; init; }
    public string OriginalValue { get; init; } = string.Empty;
    public string ImputedValue { get; init; } = string.Empty;
    public double Confidence { get; init; }
}

/// <summary>
/// Format normalization result.
/// </summary>
public record NormalizationResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ColumnName { get; init; } = string.Empty;
    public NormalizationType Type { get; init; }
    public int ValuesNormalized { get; init; }
    public string SourceFormat { get; init; } = string.Empty;
    public string TargetFormat { get; init; } = string.Empty;
    public IReadOnlyList<FormatConversion> Conversions { get; init; } = [];
}

/// <summary>
/// Single format conversion detail.
/// </summary>
public record FormatConversion
{
    public int RowIndex { get; init; }
    public string OriginalValue { get; init; } = string.Empty;
    public string NormalizedValue { get; init; } = string.Empty;
}

/// <summary>
/// Cleaning session representing a complete cleaning workflow.
/// </summary>
public record CleaningSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DatasetId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public CleaningSessionStatus Status { get; init; } = CleaningSessionStatus.Draft;
    public int Version { get; init; } = 1;
    public Guid? ParentSessionId { get; init; }
    public DataProfile? InitialProfile { get; init; }
    public DataProfile? FinalProfile { get; init; }
    public IReadOnlyList<CleaningOperation> Operations { get; init; } = [];
    public IReadOnlyList<CleaningOperationResult> Results { get; init; } = [];
    public CleaningImpactMetrics? OverallImpact { get; init; }
    public string? OutputFilePath { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

public enum CleaningOperationType
{
    // Missing Value Operations
    DropMissingRows,
    DropMissingColumns,
    ImputeMean,
    ImputeMedian,
    ImputeMode,
    ImputeConstant,
    ImputeKnn,
    ImputeRegression,
    ImputeTimeSeries,
    ImputeForwardFill,
    ImputeBackwardFill,
    
    // Outlier Operations
    RemoveOutliers,
    CapOutliers,
    TransformOutliers,
    
    // Format Normalization
    NormalizeDateTime,
    NormalizeCurrency,
    NormalizeUnits,
    NormalizeEncoding,
    NormalizeWhitespace,
    NormalizeCase,
    
    // Type Conversion
    ConvertType,
    ParseDates,
    ParseNumbers,
    
    // Text Cleaning
    RemoveStopwords,
    CorrectSpelling,
    RemoveDuplicates,
    TrimWhitespace,
    
    // Advanced
    ApplyCustomRule,
    ApplyRegex,
    ApplyPipeline
}

public enum ImputationStrategy
{
    Mean,
    Median,
    Mode,
    Constant,
    Knn,
    Regression,
    TimeSeriesInterpolation,
    ForwardFill,
    BackwardFill,
    DomainSpecific
}

public enum NormalizationType
{
    DateTime,
    Currency,
    Units,
    Encoding,
    Locale,
    PhoneNumber,
    Email,
    Address
}

public enum CleaningSessionStatus
{
    Draft,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

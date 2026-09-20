namespace AIMRAN_Data_Science_Lab.Models.DataCleaning;

/// <summary>
/// Complete profile analysis of a dataset.
/// </summary>
public record DataProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DatasetId { get; init; }
    public string DatasetName { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int TotalColumns { get; init; }
    public long SizeBytes { get; init; }
    public DataQualityScore QualityScore { get; init; } = new();
    public IReadOnlyList<ColumnProfile> Columns { get; init; } = [];
    public IReadOnlyDictionary<string, double> CorrelationMatrix { get; init; } = new Dictionary<string, double>();
    public DataDriftInfo? DriftInfo { get; init; }
    public DateTime ProfiledAt { get; init; } = DateTime.UtcNow;
    public TimeSpan ProfilingDuration { get; init; }
}

/// <summary>
/// Detailed profile for a single column.
/// </summary>
public record ColumnProfile
{
    public string Name { get; init; } = string.Empty;
    public int Index { get; init; }
    public ColumnDataType DataType { get; init; }
    public ColumnDataType? InferredType { get; init; }
    
    // Basic Statistics
    public int TotalCount { get; init; }
    public int MissingCount { get; init; }
    public double MissingPercentage => TotalCount > 0 ? (double)MissingCount / TotalCount * 100 : 0;
    public int UniqueCount { get; init; }
    public double UniquePercentage => TotalCount > 0 ? (double)UniqueCount / TotalCount * 100 : 0;
    public int DuplicateCount => TotalCount - UniqueCount;
    
    // Numeric Statistics (nullable for non-numeric columns)
    public double? Mean { get; init; }
    public double? Median { get; init; }
    public double? Mode { get; init; }
    public double? StandardDeviation { get; init; }
    public double? Variance { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Range => Max - Min;
    public double? Skewness { get; init; }
    public double? Kurtosis { get; init; }
    public IReadOnlyList<double>? Percentiles { get; init; } // 25th, 50th, 75th
    public double? IQR => Percentiles?.Count >= 3 ? Percentiles[2] - Percentiles[0] : null;
    
    // Distribution
    public IReadOnlyList<DistributionBucket> Distribution { get; init; } = [];
    public IReadOnlyList<ValueFrequency> TopValues { get; init; } = [];
    
    // Quality Indicators
    public double ColumnQualityScore { get; init; }
    public IReadOnlyList<DataQualityIssue> Issues { get; init; } = [];
    public int OutlierCount { get; init; }
    public bool HasMixedTypes { get; init; }
    public bool IsConstant => UniqueCount <= 1;
    public bool IsId => UniqueCount == TotalCount && !string.IsNullOrEmpty(Name) && 
                        (Name.Contains("id", StringComparison.OrdinalIgnoreCase) || 
                         Name.Contains("key", StringComparison.OrdinalIgnoreCase));
    
    // Text-specific (for string columns)
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public double? AvgLength { get; init; }
    public bool? ContainsEmptyStrings { get; init; }
    public bool? ContainsWhitespaceOnly { get; init; }
    
    // Date-specific (for datetime columns)
    public DateTime? MinDate { get; init; }
    public DateTime? MaxDate { get; init; }
    public bool? HasFutureDates { get; init; }
    
    // Recommendations
    public IReadOnlyList<CleaningRecommendation> Recommendations { get; init; } = [];
}

/// <summary>
/// Data quality score with breakdown.
/// </summary>
public record DataQualityScore
{
    public double OverallScore { get; init; }
    public double CompletenessScore { get; init; }
    public double AccuracyScore { get; init; }
    public double ConsistencyScore { get; init; }
    public double UniquenessScore { get; init; }
    public double ValidityScore { get; init; }
    public string Grade => OverallScore switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
    public string Summary { get; init; } = string.Empty;
}

/// <summary>
/// Detected data quality issue.
/// </summary>
public record DataQualityIssue
{
    public DataQualityIssueType Type { get; init; }
    public DataQualitySeverity Severity { get; init; }
    public string Description { get; init; } = string.Empty;
    public int AffectedCount { get; init; }
    public double AffectedPercentage { get; init; }
    public string? SuggestedFix { get; init; }
}

/// <summary>
/// AI-generated cleaning recommendation.
/// </summary>
public record CleaningRecommendation
{
    public CleaningOperationType OperationType { get; init; }
    public string Description { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
    public string Rationale { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> SuggestedParameters { get; init; } = new Dictionary<string, object>();
    public double? EstimatedImpact { get; init; }
}

/// <summary>
/// Distribution bucket for histograms.
/// </summary>
public record DistributionBucket
{
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
    public int Count { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// Value frequency for categorical analysis.
/// </summary>
public record ValueFrequency
{
    public string Value { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// Data drift detection information.
/// </summary>
public record DataDriftInfo
{
    public Guid BaselineProfileId { get; init; }
    public double DriftScore { get; init; }
    public bool HasSignificantDrift => DriftScore > 0.1;
    public IReadOnlyList<ColumnDrift> ColumnDrifts { get; init; } = [];
}

/// <summary>
/// Drift information for a single column.
/// </summary>
public record ColumnDrift
{
    public string ColumnName { get; init; } = string.Empty;
    public double DriftScore { get; init; }
    public DriftType Type { get; init; }
    public string Description { get; init; } = string.Empty;
}

public enum ColumnDataType
{
    Unknown,
    Integer,
    Float,
    Boolean,
    String,
    DateTime,
    Categorical,
    Binary,
    Json,
    Guid
}

public enum DataQualityIssueType
{
    MissingValues,
    Outliers,
    Duplicates,
    InconsistentFormat,
    InvalidValues,
    MixedTypes,
    HighCardinality,
    LowVariance,
    ImbalancedDistribution,
    DataDrift,
    EncodingIssues,
    WhitespaceIssues
}

public enum DataQualitySeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum DriftType
{
    None,
    DistributionShift,
    SchemaChange,
    ValueRangeChange,
    MissingPatternChange
}

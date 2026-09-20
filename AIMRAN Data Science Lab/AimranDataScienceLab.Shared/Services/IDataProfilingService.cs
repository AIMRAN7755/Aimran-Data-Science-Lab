using AIMRAN_Data_Science_Lab.Models.DataCleaning;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for automated data profiling and quality analysis.
/// Provides column type detection, distribution analysis, and quality scoring.
/// </summary>
public interface IDataProfilingService
{
    #region Full Profiling

    /// <summary>
    /// Generate a complete profile for a dataset including all columns.
    /// </summary>
    Task<DataProfile> ProfileDatasetAsync(
        Guid datasetId,
        ProfilingOptions? options = null,
        IProgress<ProfilingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Profile specific columns only.
    /// </summary>
    Task<IReadOnlyList<ColumnProfile>> ProfileColumnsAsync(
        Guid datasetId,
        IEnumerable<string> columns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick profile for overview without full statistics.
    /// </summary>
    Task<DataProfile> QuickProfileAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Column Analysis

    /// <summary>
    /// Detect the data type of a column with confidence scoring.
    /// </summary>
    Task<ColumnTypeDetection> DetectColumnTypeAsync(
        Guid datasetId,
        string column,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get distribution analysis for a numeric column.
    /// </summary>
    Task<DistributionAnalysis> AnalyzeDistributionAsync(
        Guid datasetId,
        string column,
        int bucketCount = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get value frequency analysis for a categorical column.
    /// </summary>
    Task<FrequencyAnalysis> AnalyzeFrequencyAsync(
        Guid datasetId,
        string column,
        int topN = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze text patterns in a string column.
    /// </summary>
    Task<TextPatternAnalysis> AnalyzeTextPatternsAsync(
        Guid datasetId,
        string column,
        CancellationToken cancellationToken = default);

    #endregion

    #region Quality Assessment

    /// <summary>
    /// Calculate overall data quality score for a dataset.
    /// </summary>
    Task<DataQualityScore> CalculateQualityScoreAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect all data quality issues in a dataset.
    /// </summary>
    Task<IReadOnlyList<DataQualityIssue>> DetectQualityIssuesAsync(
        Guid datasetId,
        DataQualitySeverity minimumSeverity = DataQualitySeverity.Warning,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI-powered cleaning recommendations based on profile.
    /// </summary>
    Task<IReadOnlyList<CleaningRecommendation>> GetCleaningRecommendationsAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Correlation & Relationships

    /// <summary>
    /// Calculate correlation matrix for numeric columns.
    /// </summary>
    Task<CorrelationMatrix> CalculateCorrelationMatrixAsync(
        Guid datasetId,
        CorrelationMethod method = CorrelationMethod.Pearson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find highly correlated column pairs.
    /// </summary>
    Task<IReadOnlyList<ColumnCorrelation>> FindHighCorrelationsAsync(
        Guid datasetId,
        double threshold = 0.8,
        CancellationToken cancellationToken = default);

    #endregion

    #region Data Drift

    /// <summary>
    /// Detect data drift between current and baseline profile.
    /// </summary>
    Task<DataDriftInfo> DetectDataDriftAsync(
        Guid currentDatasetId,
        Guid baselineDatasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare two profiles and identify differences.
    /// </summary>
    Task<ProfileComparison> CompareProfilesAsync(
        DataProfile profile1,
        DataProfile profile2,
        CancellationToken cancellationToken = default);

    #endregion

    #region Profile Management

    /// <summary>
    /// Save a profile for future reference.
    /// </summary>
    Task<DataProfile> SaveProfileAsync(
        DataProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get saved profiles for a dataset.
    /// </summary>
    Task<IReadOnlyList<DataProfile>> GetSavedProfilesAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest profile for a dataset.
    /// </summary>
    Task<DataProfile?> GetLatestProfileAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Options for profiling operations.
/// </summary>
public record ProfilingOptions
{
    public bool CalculateCorrelations { get; init; } = true;
    public bool DetectOutliers { get; init; } = true;
    public bool AnalyzeDistributions { get; init; } = true;
    public bool GenerateRecommendations { get; init; } = true;
    public int DistributionBuckets { get; init; } = 20;
    public int TopValuesCount { get; init; } = 10;
    public int? SampleSize { get; init; }
    public double? PercentilesToCalculate { get; init; }
}

/// <summary>
/// Progress information for profiling operations.
/// </summary>
public record ProfilingProgress
{
    public int CurrentColumn { get; init; }
    public int TotalColumns { get; init; }
    public string CurrentColumnName { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    public double PercentComplete { get; init; }
}

/// <summary>
/// Result of column type detection.
/// </summary>
public record ColumnTypeDetection
{
    public string ColumnName { get; init; } = string.Empty;
    public ColumnDataType DetectedType { get; init; }
    public double Confidence { get; init; }
    public IReadOnlyList<TypeCandidate> Candidates { get; init; } = [];
    public bool HasMixedTypes { get; init; }
    public string? DetectedFormat { get; init; }
}

/// <summary>
/// A candidate type with confidence score.
/// </summary>
public record TypeCandidate
{
    public ColumnDataType Type { get; init; }
    public double Confidence { get; init; }
    public int MatchingValues { get; init; }
    public string? Format { get; init; }
}

/// <summary>
/// Distribution analysis for numeric columns.
/// </summary>
public record DistributionAnalysis
{
    public string ColumnName { get; init; } = string.Empty;
    public DistributionType DetectedDistribution { get; init; }
    public double DistributionFitScore { get; init; }
    public IReadOnlyList<DistributionBucket> Histogram { get; init; } = [];
    public double Skewness { get; init; }
    public double Kurtosis { get; init; }
    public bool IsNormal { get; init; }
    public bool IsUniform { get; init; }
    public bool IsBimodal { get; init; }
}

/// <summary>
/// Frequency analysis for categorical columns.
/// </summary>
public record FrequencyAnalysis
{
    public string ColumnName { get; init; } = string.Empty;
    public int UniqueCount { get; init; }
    public int TotalCount { get; init; }
    public double Cardinality => TotalCount > 0 ? (double)UniqueCount / TotalCount : 0;
    public IReadOnlyList<ValueFrequency> TopValues { get; init; } = [];
    public IReadOnlyList<ValueFrequency> RareValues { get; init; } = [];
    public bool IsHighCardinality => Cardinality > 0.9;
    public bool IsLowCardinality => UniqueCount < 10;
}

/// <summary>
/// Text pattern analysis results.
/// </summary>
public record TextPatternAnalysis
{
    public string ColumnName { get; init; } = string.Empty;
    public IReadOnlyList<DetectedPattern> Patterns { get; init; } = [];
    public double AverageLength { get; init; }
    public int MinLength { get; init; }
    public int MaxLength { get; init; }
    public bool ContainsEmails { get; init; }
    public bool ContainsUrls { get; init; }
    public bool ContainsPhoneNumbers { get; init; }
    public bool ContainsDates { get; init; }
    public string? DetectedLanguage { get; init; }
}

/// <summary>
/// A detected text pattern.
/// </summary>
public record DetectedPattern
{
    public string Pattern { get; init; } = string.Empty;
    public string RegexPattern { get; init; } = string.Empty;
    public int MatchCount { get; init; }
    public double MatchPercentage { get; init; }
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Correlation matrix for numeric columns.
/// </summary>
public record CorrelationMatrix
{
    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<double>> Values { get; init; } = [];
    public CorrelationMethod Method { get; init; }
}

/// <summary>
/// Correlation between two columns.
/// </summary>
public record ColumnCorrelation
{
    public string Column1 { get; init; } = string.Empty;
    public string Column2 { get; init; } = string.Empty;
    public double Correlation { get; init; }
    public CorrelationStrength Strength { get; init; }
    public CorrelationDirection Direction { get; init; }
}

/// <summary>
/// Comparison between two profiles.
/// </summary>
public record ProfileComparison
{
    public Guid Profile1Id { get; init; }
    public Guid Profile2Id { get; init; }
    public IReadOnlyList<ColumnComparison> ColumnComparisons { get; init; } = [];
    public double OverallSimilarity { get; init; }
    public IReadOnlyList<string> AddedColumns { get; init; } = [];
    public IReadOnlyList<string> RemovedColumns { get; init; } = [];
    public IReadOnlyList<string> ModifiedColumns { get; init; } = [];
}

/// <summary>
/// Comparison of a single column between profiles.
/// </summary>
public record ColumnComparison
{
    public string ColumnName { get; init; } = string.Empty;
    public double StatisticalDifference { get; init; }
    public bool TypeChanged { get; init; }
    public double MissingRateDifference { get; init; }
    public double MeanDifference { get; init; }
    public double StdDevDifference { get; init; }
}

public enum DistributionType
{
    Normal,
    Uniform,
    Exponential,
    Bimodal,
    Skewed,
    Unknown
}

public enum CorrelationMethod
{
    Pearson,
    Spearman,
    Kendall
}

public enum CorrelationStrength
{
    None,
    Weak,
    Moderate,
    Strong,
    VeryStrong
}

public enum CorrelationDirection
{
    Positive,
    Negative
}

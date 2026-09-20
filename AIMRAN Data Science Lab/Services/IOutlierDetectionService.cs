using AIMRAN_Data_Science_Lab.Models.DataCleaning;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for detecting outliers using multiple algorithms.
/// Supports impact simulation and batch processing.
/// </summary>
public interface IOutlierDetectionService
{
    #region Detection Methods

    /// <summary>
    /// Detect outliers using the specified method.
    /// </summary>
    Task<OutlierDetectionResult> DetectOutliersAsync(
        Guid datasetId,
        OutlierDetectionMethod method,
        OutlierDetectionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers in specific columns only.
    /// </summary>
    Task<OutlierDetectionResult> DetectOutliersInColumnsAsync(
        Guid datasetId,
        IEnumerable<string> columns,
        OutlierDetectionMethod method,
        OutlierDetectionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers using Z-score method.
    /// </summary>
    Task<OutlierDetectionResult> DetectWithZScoreAsync(
        Guid datasetId,
        double threshold = 3.0,
        bool useModifiedZScore = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers using IQR (Interquartile Range) method.
    /// </summary>
    Task<OutlierDetectionResult> DetectWithIQRAsync(
        Guid datasetId,
        double multiplier = 1.5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers using Isolation Forest algorithm.
    /// </summary>
    Task<OutlierDetectionResult> DetectWithIsolationForestAsync(
        Guid datasetId,
        double contamination = 0.1,
        int numberOfTrees = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers using DBSCAN clustering.
    /// </summary>
    Task<OutlierDetectionResult> DetectWithDBSCANAsync(
        Guid datasetId,
        double epsilon = 0.5,
        int minSamples = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers using Local Outlier Factor.
    /// </summary>
    Task<OutlierDetectionResult> DetectWithLOFAsync(
        Guid datasetId,
        int numberOfNeighbors = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers using ensemble of multiple methods.
    /// </summary>
    Task<OutlierDetectionResult> DetectWithEnsembleAsync(
        Guid datasetId,
        IEnumerable<OutlierDetectionMethod>? methods = null,
        double consensusThreshold = 0.5,
        CancellationToken cancellationToken = default);

    #endregion

    #region Impact Simulation

    /// <summary>
    /// Simulate the impact of removing outliers on model performance.
    /// </summary>
    Task<OutlierImpactSimulation> SimulateRemovalImpactAsync(
        Guid datasetId,
        OutlierDetectionResult detectionResult,
        Guid? modelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate scenarios for different outlier removal strategies.
    /// </summary>
    Task<IReadOnlyList<OutlierScenario>> GenerateRemovalScenariosAsync(
        Guid datasetId,
        OutlierDetectionResult detectionResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare results from different detection methods.
    /// </summary>
    Task<OutlierMethodComparison> CompareDetectionMethodsAsync(
        Guid datasetId,
        IEnumerable<OutlierDetectionMethod> methods,
        CancellationToken cancellationToken = default);

    #endregion

    #region Anomaly Detection

    /// <summary>
    /// Detect anomalous records (logical inconsistencies, rare patterns).
    /// </summary>
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(
        Guid datasetId,
        AnomalyDetectionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate risk scores for each record.
    /// </summary>
    Task<IReadOnlyList<RecordRiskScore>> CalculateRiskScoresAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    #endregion

    #region History & Tracking

    /// <summary>
    /// Get history of outlier detection runs for a dataset.
    /// </summary>
    Task<IReadOnlyList<OutlierDetectionResult>> GetDetectionHistoryAsync(
        Guid datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save detection result for future reference.
    /// </summary>
    Task<OutlierDetectionResult> SaveDetectionResultAsync(
        OutlierDetectionResult result,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Options for outlier detection operations.
/// </summary>
public record OutlierDetectionOptions
{
    public IReadOnlyList<string>? TargetColumns { get; init; }
    public bool IncludeNonNumeric { get; init; }
    public double? Threshold { get; init; }
    public bool CalculateSeverity { get; init; } = true;
    public bool GenerateRecommendations { get; init; } = true;
    public int? MaxOutliersPerColumn { get; init; }
    public bool RunImpactSimulation { get; init; }
}

/// <summary>
/// Options for anomaly detection.
/// </summary>
public record AnomalyDetectionOptions
{
    public bool DetectLogicalInconsistencies { get; init; } = true;
    public bool DetectRarePatterns { get; init; } = true;
    public bool DetectSuspiciousValues { get; init; } = true;
    public double RiskThreshold { get; init; } = 0.7;
    public IReadOnlyList<AnomalyRule>? CustomRules { get; init; }
}

/// <summary>
/// Custom rule for anomaly detection.
/// </summary>
public record AnomalyRule
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> Columns { get; init; } = [];
    public string Condition { get; init; } = string.Empty;
    public double RiskScore { get; init; }
}

/// <summary>
/// Comparison of different outlier detection methods.
/// </summary>
public record OutlierMethodComparison
{
    public Guid DatasetId { get; init; }
    public IReadOnlyList<MethodComparisonResult> Results { get; init; } = [];
    public IReadOnlyList<int> ConsensusOutlierIndices { get; init; } = [];
    public double AverageAgreement { get; init; }
    public OutlierDetectionMethod RecommendedMethod { get; init; }
    public string RecommendationRationale { get; init; } = string.Empty;
}

/// <summary>
/// Result from a single method in comparison.
/// </summary>
public record MethodComparisonResult
{
    public OutlierDetectionMethod Method { get; init; }
    public int OutliersDetected { get; init; }
    public double OutlierPercentage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public IReadOnlyList<int> OutlierIndices { get; init; } = [];
    public double AgreementWithConsensus { get; init; }
}

/// <summary>
/// Risk score for a single record.
/// </summary>
public record RecordRiskScore
{
    public int RowIndex { get; init; }
    public double OverallRiskScore { get; init; }
    public IReadOnlyDictionary<string, double> ColumnRiskScores { get; init; } = new Dictionary<string, double>();
    public IReadOnlyList<string> RiskFactors { get; init; } = [];
    public RiskLevel Level { get; init; }
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

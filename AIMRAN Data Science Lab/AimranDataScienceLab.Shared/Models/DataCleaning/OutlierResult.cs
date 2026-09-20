namespace AIMRAN_Data_Science_Lab.Models.DataCleaning;

/// <summary>
/// Result of outlier detection for a dataset or column.
/// </summary>
public record OutlierDetectionResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DatasetId { get; init; }
    public OutlierDetectionMethod Method { get; init; }
    public IReadOnlyList<ColumnOutlierResult> ColumnResults { get; init; } = [];
    public int TotalOutliersDetected { get; init; }
    public double OutlierPercentage { get; init; }
    public OutlierImpactSimulation? ImpactSimulation { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan DetectionDuration { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Outlier detection results for a single column.
/// </summary>
public record ColumnOutlierResult
{
    public string ColumnName { get; init; } = string.Empty;
    public int OutlierCount { get; init; }
    public double OutlierPercentage { get; init; }
    public IReadOnlyList<OutlierRecord> Outliers { get; init; } = [];
    public OutlierBounds? Bounds { get; init; }
    public OutlierSeverityDistribution SeverityDistribution { get; init; } = new();
}

/// <summary>
/// Single outlier record.
/// </summary>
public record OutlierRecord
{
    public int RowIndex { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public double Value { get; init; }
    public double OutlierScore { get; init; }
    public OutlierSeverity Severity { get; init; }
    public OutlierDirection Direction { get; init; }
    public double DeviationFromMean { get; init; }
    public double? ZScore { get; init; }
    public string? SuggestedAction { get; init; }
}

/// <summary>
/// Bounds used for outlier detection.
/// </summary>
public record OutlierBounds
{
    public double LowerBound { get; init; }
    public double UpperBound { get; init; }
    public double? Q1 { get; init; }
    public double? Q3 { get; init; }
    public double? IQR { get; init; }
    public double? Mean { get; init; }
    public double? StandardDeviation { get; init; }
}

/// <summary>
/// Distribution of outlier severities.
/// </summary>
public record OutlierSeverityDistribution
{
    public int Mild { get; init; }
    public int Moderate { get; init; }
    public int Severe { get; init; }
    public int Extreme { get; init; }
}

/// <summary>
/// Simulated impact of removing outliers.
/// </summary>
public record OutlierImpactSimulation
{
    public double EstimatedAccuracyChange { get; init; }
    public double EstimatedPrecisionChange { get; init; }
    public double EstimatedRecallChange { get; init; }
    public double DataLossPercentage { get; init; }
    public string Recommendation { get; init; } = string.Empty;
    public IReadOnlyList<OutlierScenario> Scenarios { get; init; } = [];
}

/// <summary>
/// A specific outlier removal scenario.
/// </summary>
public record OutlierScenario
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OutlierSeverity MinSeverityToRemove { get; init; }
    public int OutliersRemoved { get; init; }
    public double DataRetentionPercentage { get; init; }
    public double EstimatedImpact { get; init; }
}

/// <summary>
/// Anomaly detection result for suspicious records.
/// </summary>
public record AnomalyDetectionResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DatasetId { get; init; }
    public IReadOnlyList<AnomalyRecord> Anomalies { get; init; } = [];
    public int TotalAnomaliesDetected { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// A detected anomaly record.
/// </summary>
public record AnomalyRecord
{
    public int RowIndex { get; init; }
    public double RiskScore { get; init; }
    public AnomalyType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> AffectedColumns { get; init; } = [];
    public IReadOnlyDictionary<string, object> Evidence { get; init; } = new Dictionary<string, object>();
    public string? SuggestedAction { get; init; }
}

public enum OutlierDetectionMethod
{
    ZScore,
    ModifiedZScore,
    IQR,
    IsolationForest,
    DBSCAN,
    LocalOutlierFactor,
    RobustCovariance,
    OneClassSVM,
    Ensemble
}

public enum OutlierSeverity
{
    Mild,
    Moderate,
    Severe,
    Extreme
}

public enum OutlierDirection
{
    Low,
    High
}

public enum AnomalyType
{
    LogicalInconsistency,
    RarePattern,
    SuspiciousValue,
    FraudIndicator,
    DataEntryError,
    SystemGlitch,
    DuplicateSuspect
}

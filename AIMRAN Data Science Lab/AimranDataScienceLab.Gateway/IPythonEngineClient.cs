namespace AimranDataScienceLab.Gateway;

/// <summary>
/// Client for the Python FastAPI AI Engine.
/// Handles experiment execution, model training, data profiling, and cleaning intelligence.
/// </summary>
public interface IPythonEngineClient
{
    #region Health

    /// <summary>
    /// Check if the Python engine is reachable and healthy.
    /// </summary>
    Task<EngineHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Experiment Operations

    /// <summary>
    /// Submit an experiment for execution on the Python engine.
    /// </summary>
    Task<PythonExperimentResult> SubmitExperimentAsync(
        SubmitExperimentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of a running experiment.
    /// </summary>
    Task<PythonExperimentStatus> GetExperimentStatusAsync(
        string experimentRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a running experiment.
    /// </summary>
    Task CancelExperimentAsync(
        string experimentRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream experiment metrics as they are produced.
    /// </summary>
    IAsyncEnumerable<ExperimentMetricUpdate> StreamMetricsAsync(
        string experimentRunId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Model Operations

    /// <summary>
    /// Train a model using the Python engine.
    /// </summary>
    Task<PythonTrainResult> TrainModelAsync(
        TrainModelRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluate a model against a test dataset.
    /// </summary>
    Task<PythonEvalResult> EvaluateModelAsync(
        string modelPath,
        string testDatasetPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run prediction using a trained model.
    /// </summary>
    Task<PythonPredictionResult> PredictAsync(
        string modelPath,
        IReadOnlyList<IReadOnlyDictionary<string, object>> inputData,
        CancellationToken cancellationToken = default);

    #endregion

    #region Data Profiling

    /// <summary>
    /// Profile a dataset using the Python engine's statistical capabilities.
    /// </summary>
    Task<PythonProfileResult> ProfileDatasetAsync(
        string datasetPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect outliers using advanced ML algorithms in Python.
    /// </summary>
    Task<PythonOutlierResult> DetectOutliersAsync(
        string datasetPath,
        string method,
        IReadOnlyDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Cleaning Intelligence

    /// <summary>
    /// Get AI-recommended cleaning operations for a dataset.
    /// </summary>
    Task<PythonCleaningRecommendation> GetCleaningRecommendationsAsync(
        string datasetPath,
        object? existingProfile = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Code Execution

    /// <summary>
    /// Execute a Python code snippet on the AI engine.
    /// </summary>
    Task<PythonCodeExecutionResult> ExecuteCodeAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Package Management

    /// <summary>
    /// List all installed pip packages.
    /// </summary>
    Task<PythonPackageListResult> ListPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Install a pip package.
    /// </summary>
    Task<PythonPackageActionResult> InstallPackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall a pip package.
    /// </summary>
    Task<PythonPackageActionResult> UninstallPackageAsync(
        string packageName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Visualization

    /// <summary>
    /// Generate a chart image from a dataset.
    /// </summary>
    Task<PythonVisualizationResult> GenerateChartAsync(
        PythonVisualizationRequest request,
        CancellationToken cancellationToken = default);

    #endregion
}

#region Python Engine DTOs

/// <summary>
/// Request payload for submitting an experiment to the Python engine.
/// </summary>
public sealed record SubmitExperimentRequest
{
    public required Guid ExperimentId { get; init; }
    public required string Name { get; init; }
    public required string DatasetPath { get; init; }
    public IReadOnlyDictionary<string, object>? Hyperparameters { get; init; }
    public string ComputeTarget { get; init; } = "local";
}

public sealed record PythonExperimentResult
{
    public required string RunId { get; init; }
    public bool Accepted { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record PythonExperimentStatus
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
    public double? Progress { get; init; }
    public IReadOnlyDictionary<string, double>? CurrentMetrics { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record ExperimentMetricUpdate
{
    public required string MetricName { get; init; }
    public double Value { get; init; }
    public int Epoch { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed record TrainModelRequest
{
    public required string DatasetPath { get; init; }
    public required string Algorithm { get; init; }
    public required string Framework { get; init; }
    public IReadOnlyDictionary<string, object>? Hyperparameters { get; init; }
    public double TrainTestSplit { get; init; } = 0.8;
}

public sealed record PythonTrainResult
{
    public required string ModelPath { get; init; }
    public required IReadOnlyDictionary<string, double> Metrics { get; init; }
    public TimeSpan TrainingDuration { get; init; }
}

public sealed record PythonEvalResult
{
    public required IReadOnlyDictionary<string, double> Metrics { get; init; }
    public int TestSampleCount { get; init; }
}

public sealed record PythonPredictionResult
{
    public required IReadOnlyList<object> Predictions { get; init; }
    public IReadOnlyList<double>? Probabilities { get; init; }
}

public sealed record PythonProfileResult
{
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public required IReadOnlyList<PythonColumnProfile> Columns { get; init; }
    public double QualityScore { get; init; }
}

public sealed record PythonColumnProfile
{
    public required string Name { get; init; }
    public required string DataType { get; init; }
    public int NullCount { get; init; }
    public double NullPercentage { get; init; }
    public int UniqueCount { get; init; }
    public double? Mean { get; init; }
    public double? StdDev { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
}

public sealed record PythonOutlierResult
{
    public required string Method { get; init; }
    public int OutlierCount { get; init; }
    public required IReadOnlyList<int> OutlierIndices { get; init; }
    public required IReadOnlyList<double> OutlierScores { get; init; }
}

public sealed record PythonCleaningRecommendation
{
    public required IReadOnlyList<RecommendedOperation> Operations { get; init; }
    public double EstimatedQualityImprovement { get; init; }
}

public sealed record RecommendedOperation
{
    public required string OperationType { get; init; }
    public required string TargetColumn { get; init; }
    public required string Reason { get; init; }
    public double Confidence { get; init; }
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}

public sealed record PythonCodeExecutionResult
{
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public int ExitCode { get; init; }
    public double ExecutionTimeSeconds { get; init; }
    public bool HasFigure { get; init; }
    public string? FigureBase64 { get; init; }
}

public sealed record PythonPackageInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Summary { get; init; }
    public string? LatestVersion { get; init; }
}

public sealed record PythonPackageListResult
{
    public required IReadOnlyList<PythonPackageInfo> Packages { get; init; }
}

public sealed record PythonPackageActionResult
{
    public bool Success { get; init; }
    public required string PackageName { get; init; }
    public required string Message { get; init; }
}

public sealed record PythonVisualizationRequest
{
    public required string DatasetPath { get; init; }
    public required string ChartType { get; init; }
    public string? XColumn { get; init; }
    public string? YColumn { get; init; }
    public IReadOnlyList<string>? Columns { get; init; }
    public string? Title { get; init; }
    public IReadOnlyDictionary<string, object>? Options { get; init; }
}

public sealed record PythonVisualizationResult
{
    public required string ChartType { get; init; }
    public required string FigureBase64 { get; init; }
    public required string PythonCode { get; init; }
}

#endregion

namespace AimranDataScienceLab.Gateway;

/// <summary>
/// Client for the R Plumber statistical engine.
/// Handles R code execution, package management, visualization (ggplot2), and statistical analysis.
/// </summary>
public interface IREngineClient
{
    #region Health

    /// <summary>
    /// Check if the R engine is reachable and healthy.
    /// </summary>
    Task<EngineHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Code Execution

    /// <summary>
    /// Execute an R code snippet on the Plumber engine.
    /// </summary>
    Task<RCodeExecutionResult> ExecuteCodeAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Package Management

    /// <summary>
    /// List all installed R packages.
    /// </summary>
    Task<RPackageListResult> ListPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Install an R package from CRAN.
    /// </summary>
    Task<RPackageActionResult> InstallPackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall an R package.
    /// </summary>
    Task<RPackageActionResult> UninstallPackageAsync(
        string packageName,
        CancellationToken cancellationToken = default);

    #endregion

    #region Visualization

    /// <summary>
    /// Generate a ggplot2 chart image from a dataset.
    /// </summary>
    Task<RVisualizationResult> GenerateChartAsync(
        RVisualizationRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistical Analysis

    /// <summary>
    /// Profile a dataset using R's statistical capabilities.
    /// </summary>
    Task<RProfileResult> ProfileDatasetAsync(
        string datasetPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute a correlation matrix for numeric columns.
    /// </summary>
    Task<RCorrelationResult> ComputeCorrelationAsync(
        string datasetPath,
        string method = "pearson",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run linear regression analysis.
    /// </summary>
    Task<RRegressionResult> RunRegressionAsync(
        string datasetPath,
        string targetColumn,
        IReadOnlyList<string> featureColumns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform a t-test between two columns.
    /// </summary>
    Task<RTTestResult> RunTTestAsync(
        string datasetPath,
        string columnA,
        string columnB,
        bool paired = false,
        CancellationToken cancellationToken = default);

    #endregion
}

#region R Engine DTOs

/// <summary>
/// Result of executing an R code snippet.
/// </summary>
public sealed record RCodeExecutionResult
{
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public int ExitCode { get; init; }
    public double ExecutionTimeSeconds { get; init; }
    public bool HasFigure { get; init; }
    public string? FigureBase64 { get; init; }
}

/// <summary>
/// Information about an installed R package.
/// </summary>
public sealed record RPackageInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

/// <summary>
/// Result of listing installed R packages.
/// </summary>
public sealed record RPackageListResult
{
    public required IReadOnlyList<RPackageInfo> Packages { get; init; }
}

/// <summary>
/// Result of an R package install or uninstall operation.
/// </summary>
public sealed record RPackageActionResult
{
    public bool Success { get; init; }
    public required string PackageName { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Request to generate a ggplot2 chart via the R engine.
/// </summary>
public sealed record RVisualizationRequest
{
    public required string DatasetPath { get; init; }
    public required string ChartType { get; init; }
    public string? XColumn { get; init; }
    public string? YColumn { get; init; }
    public string? Title { get; init; }
}

/// <summary>
/// Result of R chart generation.
/// </summary>
public sealed record RVisualizationResult
{
    public required string ChartType { get; init; }
    public required string FigureBase64 { get; init; }
    public required string RCode { get; init; }
}

/// <summary>
/// R engine dataset profile result.
/// </summary>
public sealed record RProfileResult
{
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public required IReadOnlyList<RColumnProfile> Columns { get; init; }
    public double QualityScore { get; init; }
}

/// <summary>
/// Per-column profile from R engine profiling.
/// </summary>
public sealed record RColumnProfile
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

/// <summary>
/// Correlation matrix result from R engine.
/// </summary>
public sealed record RCorrelationResult
{
    public required string Method { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<double>> Matrix { get; init; }
}

/// <summary>
/// Linear regression result from R engine.
/// </summary>
public sealed record RRegressionResult
{
    public double RSquared { get; init; }
    public double AdjRSquared { get; init; }
    public double FStatistic { get; init; }
    public double PValue { get; init; }
    public required IReadOnlyList<RRegressionCoefficient> Coefficients { get; init; }
}

/// <summary>
/// A single coefficient from a regression model.
/// </summary>
public sealed record RRegressionCoefficient
{
    public required string Name { get; init; }
    public double Estimate { get; init; }
    public double StdError { get; init; }
    public double TValue { get; init; }
    public double PValue { get; init; }
}

/// <summary>
/// T-test result from R engine.
/// </summary>
public sealed record RTTestResult
{
    public double Statistic { get; init; }
    public double PValue { get; init; }
    public required IReadOnlyList<double> ConfidenceInterval { get; init; }
    public required string Method { get; init; }
}

#endregion

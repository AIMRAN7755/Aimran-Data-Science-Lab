namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for generating data visualizations from datasets.
/// </summary>
public interface IVisualizationService
{
    /// <summary>
    /// Generate a chart image from a dataset.
    /// </summary>
    Task<VisualizationResult> GenerateChartAsync(
        VisualizationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to generate a chart.
/// </summary>
public record VisualizationRequest
{
    public required string DatasetPath { get; init; }
    public required string ChartType { get; init; }
    public string? XColumn { get; init; }
    public string? YColumn { get; init; }
    public IReadOnlyList<string>? Columns { get; init; }
    public string? Title { get; init; }
    public IReadOnlyDictionary<string, object>? Options { get; init; }
}

/// <summary>
/// Result of chart generation.
/// </summary>
public record VisualizationResult
{
    public required string ChartType { get; init; }
    public required string FigureBase64 { get; init; }
    public required string PythonCode { get; init; }
}

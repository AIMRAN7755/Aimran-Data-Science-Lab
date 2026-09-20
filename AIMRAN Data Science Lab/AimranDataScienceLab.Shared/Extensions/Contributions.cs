namespace AIMRAN_Data_Science_Lab.Extensions;

/// <summary>
/// Extension contribution that adds a command to the IDE command palette.
/// </summary>
public interface ICommandContribution
{
    /// <summary>
    /// Unique command identifier.
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// Display title shown in menus and palette.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Execute the command.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension contribution that provides a code analyzer for a language.
/// </summary>
public interface ICodeAnalyzerContribution
{
    /// <summary>
    /// Language this analyzer targets (e.g., "python", "r", "sql").
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Analyze code and return diagnostic results.
    /// </summary>
    Task<IReadOnlyList<CodeDiagnostic>> AnalyzeAsync(string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// A single diagnostic result from a code analyzer.
/// </summary>
public sealed record CodeDiagnostic
{
    public required string Message { get; init; }
    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Warning;
    public int Line { get; init; }
    public int Column { get; init; }
    public string? RuleId { get; init; }
}

/// <summary>
/// Severity of a code diagnostic.
/// </summary>
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Extension contribution that provides a visualization type.
/// </summary>
public interface IVisualizationContribution
{
    /// <summary>
    /// Unique chart type identifier (e.g., "correlation-matrix", "time-series").
    /// </summary>
    string ChartTypeId { get; }

    /// <summary>
    /// Display name shown in the chart builder.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Generate a chart image from the given dataset.
    /// </summary>
    Task<VisualizationOutput> GenerateAsync(
        string datasetPath,
        IReadOnlyDictionary<string, object>? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Output from a visualization contribution.
/// </summary>
public sealed record VisualizationOutput
{
    /// <summary>
    /// Base64-encoded PNG image.
    /// </summary>
    public required string ImageBase64 { get; init; }

    /// <summary>
    /// Source code (Python/R) that generated the chart, for display.
    /// </summary>
    public string? SourceCode { get; init; }
}

/// <summary>
/// Extension contribution that provides a data connector for external sources.
/// </summary>
public interface IDataConnectorContribution
{
    /// <summary>
    /// Connector identifier (e.g., "mysql", "postgresql", "mongodb").
    /// </summary>
    string ConnectorId { get; }

    /// <summary>
    /// Display name shown in the data connectors panel.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Test the connection with given parameters.
    /// </summary>
    Task<bool> TestConnectionAsync(
        IReadOnlyDictionary<string, string> connectionParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List available tables/collections from the connected source.
    /// </summary>
    Task<IReadOnlyList<string>> ListTablesAsync(
        IReadOnlyDictionary<string, string> connectionParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a query and return results as CSV text.
    /// </summary>
    Task<string> ExecuteQueryAsync(
        string query,
        IReadOnlyDictionary<string, string> connectionParams,
        CancellationToken cancellationToken = default);
}

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for executing Python code snippets in the AI engine.
/// </summary>
public interface IPythonExecutionService
{
    /// <summary>
    /// Execute a Python code snippet and return the output.
    /// </summary>
    Task<CodeExecutionResult> ExecuteAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of executing a Python code snippet.
/// </summary>
public record CodeExecutionResult
{
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public int ExitCode { get; init; }
    public double ExecutionTimeSeconds { get; init; }
    public bool HasFigure { get; init; }
    public string? FigureBase64 { get; init; }

    public bool IsSuccess => ExitCode == 0;
}

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for executing R code snippets in the R Plumber engine.
/// </summary>
public interface IRExecutionService
{
    /// <summary>
    /// Execute an R code snippet and return the output.
    /// </summary>
    Task<CodeExecutionResult> ExecuteAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}

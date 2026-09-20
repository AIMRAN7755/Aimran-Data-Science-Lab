using AIMRAN_Data_Science_Lab.Models.Auth;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Validates and enforces execution policies on code before it runs
/// in the Python/R engines. Acts as the sandbox gate-keeper.
/// </summary>
public interface IExecutionSandbox
{
    /// <summary>Validate code against the given execution policy.</summary>
    CodeValidationResult Validate(string code, ExecutionPolicy policy);

    /// <summary>
    /// Execute code through the appropriate engine with policy enforcement
    /// (timeout, resource limits, blocked-pattern checks).
    /// </summary>
    Task<CodeExecutionResult> ExecuteSecureAsync(
        string code,
        string engine,
        ExecutionPolicy? policy = null,
        CancellationToken ct = default);
}

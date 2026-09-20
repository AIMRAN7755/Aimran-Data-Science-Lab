using AIMRAN_Data_Science_Lab.Models.Auth;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// Validates code against blocked patterns, enforces timeouts, and delegates
/// execution to the appropriate engine. Serves as the sandbox gate-keeper.
/// </summary>
internal sealed class SecureExecutionService : IExecutionSandbox
{
    private readonly IPythonExecutionService _python;
    private readonly IRExecutionService _r;
    private readonly IAuditService _audit;
    private readonly IAuthorizationService _authz;
    private readonly ILogger<SecureExecutionService> _logger;

    public SecureExecutionService(
        IPythonExecutionService python,
        IRExecutionService r,
        IAuditService audit,
        IAuthorizationService authz,
        ILogger<SecureExecutionService> logger)
    {
        _python = python;
        _r = r;
        _audit = audit;
        _authz = authz;
        _logger = logger;
    }

    public CodeValidationResult Validate(string code, ExecutionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(code))
            return CodeValidationResult.Allowed;

        var violations = new List<string>();

        foreach (var pattern in policy.BlockedPatterns)
        {
            if (code.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"Blocked pattern detected: '{pattern}'");
            }
        }

        return violations.Count > 0
            ? CodeValidationResult.Denied(violations)
            : CodeValidationResult.Allowed;
    }

    public async Task<CodeExecutionResult> ExecuteSecureAsync(
        string code,
        string engine,
        ExecutionPolicy? policy = null,
        CancellationToken ct = default)
    {
        policy ??= ExecutionPolicy.Notebook;

        // Authorize
        var permission = engine.Equals("python", StringComparison.OrdinalIgnoreCase)
            ? AimranPermission.ExecutePython
            : AimranPermission.ExecuteR;
        await _authz.RequirePermissionAsync(permission, ct);

        // Validate code
        var validation = Validate(code, policy);
        if (!validation.IsAllowed)
        {
            _logger.LogWarning("Code execution blocked: {Violations}",
                string.Join("; ", validation.Violations));

            await _audit.LogAsync("ExecutionBlocked",
                $"Engine={engine}; Violations={string.Join(", ", validation.Violations)}", ct);

            return new CodeExecutionResult
            {
                Stdout = "",
                Stderr = $"Execution blocked: {string.Join("; ", validation.Violations)}",
                ExitCode = -1,
                ExecutionTimeSeconds = 0
            };
        }

        // Enforce timeout via linked cancellation
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(policy.MaxDuration);

        var timeoutSeconds = (int)policy.MaxDuration.TotalSeconds;

        await _audit.LogAsync("ExecutionStart", $"Engine={engine}; TimeoutSec={timeoutSeconds}", ct);

        try
        {
            var result = engine.Equals("python", StringComparison.OrdinalIgnoreCase)
                ? await _python.ExecuteAsync(code, timeoutSeconds, cancellationToken: timeoutCts.Token)
                : await _r.ExecuteAsync(code, timeoutSeconds, cancellationToken: timeoutCts.Token);

            await _audit.LogAsync("ExecutionComplete",
                $"Engine={engine}; ExitCode={result.ExitCode}; Duration={result.ExecutionTimeSeconds:F1}s", ct);

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Execution timed out after {Timeout}", policy.MaxDuration);
            await _audit.LogAsync("ExecutionTimeout", $"Engine={engine}; Limit={policy.MaxDuration}", ct);

            return new CodeExecutionResult
            {
                Stdout = "",
                Stderr = $"Execution timed out after {policy.MaxDuration.TotalMinutes:F0} minutes.",
                ExitCode = -2,
                ExecutionTimeSeconds = policy.MaxDuration.TotalSeconds
            };
        }
    }
}

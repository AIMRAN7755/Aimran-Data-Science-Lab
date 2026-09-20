using AIMRAN_Data_Science_Lab.Services;
using AimranDataScienceLab.Gateway;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Executes R code via the Gateway's R Plumber Engine.
/// </summary>
internal sealed class GatewayRExecutionService : IRExecutionService
{
    private readonly IGatewayManager _gateway;

    public GatewayRExecutionService(IGatewayManager gateway)
    {
        _gateway = gateway;
    }

    public async Task<CodeExecutionResult> ExecuteAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _gateway.REngine.ExecuteCodeAsync(code, timeoutSeconds, workingDirectory, cancellationToken);
        return new CodeExecutionResult
        {
            Stdout = result.Stdout,
            Stderr = result.Stderr,
            ExitCode = result.ExitCode,
            ExecutionTimeSeconds = result.ExecutionTimeSeconds,
            HasFigure = result.HasFigure,
            FigureBase64 = result.FigureBase64
        };
    }
}

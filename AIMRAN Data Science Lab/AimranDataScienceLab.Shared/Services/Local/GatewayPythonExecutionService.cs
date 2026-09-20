using AIMRAN_Data_Science_Lab.Services;
using AimranDataScienceLab.Gateway;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Executes Python code via the Gateway's Python AI Engine.
/// </summary>
internal sealed class GatewayPythonExecutionService : IPythonExecutionService
{
    private readonly IGatewayManager _gateway;

    public GatewayPythonExecutionService(IGatewayManager gateway)
    {
        _gateway = gateway;
    }

    public async Task<CodeExecutionResult> ExecuteAsync(
        string code,
        int timeoutSeconds = 30,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _gateway.PythonEngine.ExecuteCodeAsync(code, timeoutSeconds, workingDirectory, cancellationToken);
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

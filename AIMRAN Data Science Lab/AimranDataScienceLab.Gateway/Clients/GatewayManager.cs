using AimranDataScienceLab.Gateway.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AimranDataScienceLab.Gateway.Clients;

/// <summary>
/// Coordinates health checks, process lifecycle, and provides access to engine clients.
/// </summary>
internal sealed class GatewayManager : IGatewayManager, IDisposable
{
    private readonly GatewayOptions _options;
    private readonly EngineProcessManager _processManager;
    private readonly ILogger<GatewayManager> _logger;

    public GatewayManager(
        IPythonEngineClient pythonEngine,
        IRustEngineClient rustEngine,
        IREngineClient rEngine,
        EngineProcessManager processManager,
        IOptions<GatewayOptions> options,
        ILogger<GatewayManager> logger)
    {
        PythonEngine = pythonEngine;
        RustEngine = rustEngine;
        REngine = rEngine;
        _processManager = processManager;
        _options = options.Value;
        _logger = logger;
    }

    public IPythonEngineClient PythonEngine { get; }

    public IRustEngineClient RustEngine { get; }

    public IREngineClient REngine { get; }

    public async Task<GatewayHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var pythonTask = PythonEngine.CheckHealthAsync(cancellationToken);
        var rustTask = RustEngine.CheckHealthAsync(cancellationToken);
        var rTask = REngine.CheckHealthAsync(cancellationToken);

        await Task.WhenAll(pythonTask, rustTask, rTask);

        return new GatewayHealthReport
        {
            PythonEngine = await pythonTask,
            RustEngine = await rustTask,
            REngine = await rTask,
            ProcessStates = _processManager.GetAllProcessStates()
        };
    }

    public async Task StartEnginesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting external engine processes...");
        await _processManager.StartAllAsync(cancellationToken);

        // Wait for engines to be reachable
        var deadline = DateTime.UtcNow.Add(TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline)
        {
            var health = await CheckHealthAsync(cancellationToken);
            if (health.AllHealthy)
            {
                _logger.LogInformation("All engines healthy and responding");
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        _logger.LogWarning("Engines started but not all are healthy yet");
    }

    public async Task StopEnginesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping external engine processes...");
        await _processManager.StopAllAsync(cancellationToken);
    }

    public IReadOnlyList<EngineProcessState> GetProcessStates() => _processManager.GetAllProcessStates();

    public IReadOnlyList<EngineLogEntry> GetEngineLogs(string engineKey, int maxEntries = 100) =>
        _processManager.GetLogs(engineKey, maxEntries);

    public IReadOnlyList<EngineLogEntry> GetAllEngineLogs(int maxEntries = 200) =>
        _processManager.GetAllLogs(maxEntries);

    public void Dispose()
    {
        _processManager.Dispose();
    }
}

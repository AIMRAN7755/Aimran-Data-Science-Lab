namespace AimranDataScienceLab.Gateway;

/// <summary>
/// Top-level gateway that coordinates health checks, process lifecycle,
/// and provides access to individual engine clients.
/// </summary>
public interface IGatewayManager
{
    /// <summary>
    /// Check the health of all connected engines.
    /// </summary>
    Task<GatewayHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start all configured external engine processes.
    /// </summary>
    Task StartEnginesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop all running external engine processes.
    /// </summary>
    Task StopEnginesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current process states for all engines.
    /// </summary>
    IReadOnlyList<EngineProcessState> GetProcessStates();

    /// <summary>
    /// Get captured log entries for a specific engine.
    /// </summary>
    IReadOnlyList<EngineLogEntry> GetEngineLogs(string engineKey, int maxEntries = 100);

    /// <summary>
    /// Get captured log entries for all engines combined.
    /// </summary>
    IReadOnlyList<EngineLogEntry> GetAllEngineLogs(int maxEntries = 200);

    /// <summary>
    /// Get the Python AI Engine client.
    /// </summary>
    IPythonEngineClient PythonEngine { get; }

    /// <summary>
    /// Get the Rust Resource Engine client.
    /// </summary>
    IRustEngineClient RustEngine { get; }

    /// <summary>
    /// Get the R statistical engine client.
    /// </summary>
    IREngineClient REngine { get; }
}

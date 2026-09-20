namespace AimranDataScienceLab.Gateway;

/// <summary>
/// Health status of an external engine process.
/// </summary>
public sealed record EngineHealthStatus
{
    public required string EngineName { get; init; }
    public bool IsHealthy { get; init; }
    public string? Version { get; init; }
    public TimeSpan? ResponseTime { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Aggregated gateway health across all engines.
/// </summary>
public sealed record GatewayHealthReport
{
    public bool AllHealthy => PythonEngine.IsHealthy && RustEngine.IsHealthy && REngine.IsHealthy;
    public required EngineHealthStatus PythonEngine { get; init; }
    public required EngineHealthStatus RustEngine { get; init; }
    public required EngineHealthStatus REngine { get; init; }
    public IReadOnlyList<EngineProcessState> ProcessStates { get; init; } = [];
    public DateTime ReportedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Runtime state of a managed engine process.
/// </summary>
public sealed record EngineProcessState
{
    public required string EngineKey { get; init; }
    public int? ProcessId { get; init; }
    public ProcessStatus Status { get; init; } = ProcessStatus.NotStarted;
    public DateTime? StartedAt { get; init; }
    public int RestartCount { get; init; }
    public int? LastExitCode { get; init; }
}

/// <summary>
/// Lifecycle status of a managed engine process.
/// </summary>
public enum ProcessStatus
{
    NotStarted,
    Starting,
    Running,
    Stopping,
    Stopped,
    Crashed
}

/// <summary>
/// A single log entry captured from an engine process.
/// </summary>
public sealed record EngineLogEntry
{
    public required string EngineKey { get; init; }
    public EngineLogLevel Level { get; init; }
    public required string Message { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Severity level for engine log entries.
/// </summary>
public enum EngineLogLevel
{
    Stdout,
    Stderr,
    Info,
    Warning,
    Error
}

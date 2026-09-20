namespace AimranDataScienceLab.Gateway.Configuration;

/// <summary>
/// Configuration options for the Service Gateway layer.
/// Controls connectivity to the Python AI Engine and Rust Resource Engine.
/// </summary>
public sealed record GatewayOptions
{
    public PythonEngineConfig PythonEngine { get; init; } = new();
    public RustEngineConfig RustEngine { get; init; } = new();
    public REngineConfig REngine { get; init; } = new();
    public GatewayMode Mode { get; init; } = GatewayMode.InProcess;
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxRetryAttempts { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Configuration for the Python FastAPI AI Engine.
/// </summary>
public sealed record PythonEngineConfig
{
    public string BaseUrl { get; init; } = "http://localhost:8100";
    public string HealthEndpoint { get; init; } = "/health";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
    public bool AutoStart { get; init; } = true;
    public string? PythonPath { get; init; }
    public string? ScriptPath { get; init; }
}

/// <summary>
/// Configuration for the Rust high-performance Resource Engine.
/// </summary>
public sealed record RustEngineConfig
{
    public string BaseUrl { get; init; } = "http://localhost:8200";
    public string HealthEndpoint { get; init; } = "/health";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
    public bool AutoStart { get; init; } = true;
    public string? BinaryPath { get; init; }
}

/// <summary>
/// Configuration for the R Plumber statistical engine.
/// </summary>
public sealed record REngineConfig
{
    public string BaseUrl { get; init; } = "http://localhost:9000";
    public string HealthEndpoint { get; init; } = "/health";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
    public bool AutoStart { get; init; } = true;
    public string? RscriptPath { get; init; }
    public string? PlumberScriptPath { get; init; }
}

/// <summary>
/// Determines how the gateway routes calls.
/// </summary>
public enum GatewayMode
{
    /// <summary>All processing happens in the C# process (no external engines).</summary>
    InProcess = 0,

    /// <summary>Routes AI work to Python, resource work to Rust, rest in-process.</summary>
    Hybrid = 1,

    /// <summary>All work routed to external engines where possible.</summary>
    FullDistributed = 2
}

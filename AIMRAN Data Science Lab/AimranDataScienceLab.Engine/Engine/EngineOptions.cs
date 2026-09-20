namespace AimranDataScienceLab.Engine;

public sealed record EngineOptions
{
    public EngineMode Mode { get; init; } = EngineMode.LocalFirst;

    public bool EnableAzureFeatures { get; init; } = true;

    public bool EnableDatasetVersioning { get; init; } = true;

    public bool EnableDataCleaning { get; init; } = true;

    /// <summary>
    /// Enable the Service Gateway for routing work to Python/Rust engines.
    /// </summary>
    public bool EnableGateway { get; init; }

    /// <summary>
    /// Use SQLite for persistent metadata instead of in-memory storage.
    /// </summary>
    public bool UseSqliteStorage { get; init; } = true;
}

public enum EngineMode
{
    /// <summary>All processing in-process with C#, in-memory storage.</summary>
    LocalOnly = 0,

    /// <summary>SQLite persistence, prefer local C# processing.</summary>
    LocalFirst = 1,

    /// <summary>Prefer Azure cloud for compute/storage when available.</summary>
    CloudFirst = 2,

    /// <summary>Routes AI work to Python engine, resource work to Rust engine.</summary>
    Hybrid = 3
}

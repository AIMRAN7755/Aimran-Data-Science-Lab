namespace AIMRAN_Data_Science_Lab.Models;

/// <summary>
/// Represents an installed or available Python package (pip).
/// </summary>
public record PythonPackage
{
    public required string Name { get; init; }
    public required string InstalledVersion { get; init; }
    public string? LatestVersion { get; init; }
    public bool IsOutdated => LatestVersion is not null && LatestVersion != InstalledVersion;
    public string? Summary { get; init; }
    public DateTime? InstalledAt { get; init; }
}

/// <summary>
/// Represents an installed or available Rust crate dependency.
/// </summary>
public record RustCrate
{
    public required string Name { get; init; }
    public required string InstalledVersion { get; init; }
    public string? LatestVersion { get; init; }
    public bool IsOutdated => LatestVersion is not null && LatestVersion != InstalledVersion;
    public string? Description { get; init; }
    public IReadOnlyList<string> Features { get; init; } = [];
}

/// <summary>
/// A versioned snapshot of a registered ML model.
/// </summary>
public record ModelVersionEntry
{
    public Guid ModelId { get; init; }
    public required string Name { get; init; }
    public int Version { get; init; }
    public required string Algorithm { get; init; }
    public required string Framework { get; init; }
    public long SizeBytes { get; init; }
    public ModelStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
}

/// <summary>
/// Request to install or upgrade a package.
/// </summary>
public record PackageInstallRequest
{
    public required string PackageName { get; init; }
    public string? Version { get; init; }
    public bool Upgrade { get; init; }
}

/// <summary>
/// Result of a package install/uninstall operation.
/// </summary>
public record PackageOperationResult
{
    public required string PackageName { get; init; }
    public PackageOperationStatus Status { get; init; }
    public string? InstalledVersion { get; init; }
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}

public enum PackageOperationStatus
{
    Success,
    AlreadyInstalled,
    NotFound,
    Failed,
    Cancelled
}

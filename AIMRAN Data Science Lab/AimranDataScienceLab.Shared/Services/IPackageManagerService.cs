namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing Python pip packages in the AI engine environment.
/// </summary>
public interface IPackageManagerService
{
    /// <summary>
    /// List all installed pip packages.
    /// </summary>
    Task<IReadOnlyList<PackageInfo>> ListPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Install a pip package.
    /// </summary>
    Task<PackageActionResult> InstallAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall a pip package.
    /// </summary>
    Task<PackageActionResult> UninstallAsync(
        string packageName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an installed Python package.
/// </summary>
public record PackageInfo
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Summary { get; init; }
    public string? LatestVersion { get; init; }
}

/// <summary>
/// Result of a package install or uninstall operation.
/// </summary>
public record PackageActionResult
{
    public bool Success { get; init; }
    public required string PackageName { get; init; }
    public required string Message { get; init; }
}

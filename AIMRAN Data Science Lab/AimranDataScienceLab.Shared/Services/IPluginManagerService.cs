using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing Python packages, Rust crates, and ML model versions.
/// </summary>
public interface IPluginManagerService
{
    #region Python Packages

    /// <summary>
    /// List all installed Python packages in the active environment.
    /// </summary>
    Task<IReadOnlyList<PythonPackage>> ListPythonPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Install a Python package via pip.
    /// </summary>
    Task<PackageOperationResult> InstallPythonPackageAsync(PackageInstallRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall a Python package via pip.
    /// </summary>
    Task<PackageOperationResult> UninstallPythonPackageAsync(string packageName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check which installed Python packages have newer versions available.
    /// </summary>
    Task<IReadOnlyList<PythonPackage>> CheckPythonUpdatesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Rust Crates

    /// <summary>
    /// List Rust crate dependencies from the engine's Cargo.toml.
    /// </summary>
    Task<IReadOnlyList<RustCrate>> ListRustCratesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a Rust crate dependency to the engine via cargo add.
    /// </summary>
    Task<PackageOperationResult> AddRustCrateAsync(PackageInstallRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a Rust crate dependency from the engine.
    /// </summary>
    Task<PackageOperationResult> RemoveRustCrateAsync(string crateName, CancellationToken cancellationToken = default);

    #endregion

    #region Model Versioning

    /// <summary>
    /// List all model versions grouped by model name.
    /// </summary>
    Task<IReadOnlyList<ModelVersionEntry>> ListModelVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new version of a model from a new file path.
    /// </summary>
    Task<ModelVersionEntry> CreateModelVersionAsync(Guid modelId, string newFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a specific model version.
    /// </summary>
    Task DeleteModelVersionAsync(Guid modelId, CancellationToken cancellationToken = default);

    #endregion
}

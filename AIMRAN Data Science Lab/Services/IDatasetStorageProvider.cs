using AIMRAN_Data_Science_Lab.Models.DatasetVersioning;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Abstraction for dataset version storage operations.
/// Supports local filesystem and cloud storage providers.
/// </summary>
public interface IDatasetStorageProvider
{
    /// <summary>
    /// Get the storage provider type.
    /// </summary>
    StorageProvider ProviderType { get; }

    #region File Operations

    /// <summary>
    /// Save a dataset file to versioned storage.
    /// </summary>
    Task<string> SaveDatasetAsync(
        DatasetId datasetId,
        DatasetVersionId versionId,
        string sourcePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save delta data to storage.
    /// </summary>
    Task<string> SaveDeltaAsync(
        DatasetId datasetId,
        DatasetDelta delta,
        byte[] deltaData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load a dataset file from storage.
    /// </summary>
    Task<byte[]> LoadDatasetAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load delta data from storage.
    /// </summary>
    Task<byte[]> LoadDeltaAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy a version to an output path.
    /// </summary>
    Task<string> ExportToPathAsync(
        string storagePath,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage.
    /// </summary>
    Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a path exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    #endregion

    #region Metadata Operations

    /// <summary>
    /// Save version metadata.
    /// </summary>
    Task SaveVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersion version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load version metadata.
    /// </summary>
    Task<DatasetVersion?> LoadVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load all version metadata for a dataset.
    /// </summary>
    Task<IReadOnlyList<DatasetVersion>> LoadAllVersionMetadataAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update version metadata.
    /// </summary>
    Task UpdateVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersion version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete version metadata.
    /// </summary>
    Task DeleteVersionMetadataAsync(
        DatasetId datasetId,
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Hash Operations

    /// <summary>
    /// Compute hash for a file.
    /// </summary>
    Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute hash for byte array.
    /// </summary>
    string ComputeHash(byte[] data);

    /// <summary>
    /// Verify file integrity against stored hash.
    /// </summary>
    Task<HashVerificationResult> VerifyHashAsync(
        string storagePath,
        string expectedHash,
        CancellationToken cancellationToken = default);

    #endregion

    #region Storage Management

    /// <summary>
    /// Get size of a stored file.
    /// </summary>
    Task<long> GetFileSizeAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total storage used by a dataset's versions.
    /// </summary>
    Task<long> GetTotalStorageAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all version files for a dataset.
    /// </summary>
    Task<IReadOnlyList<StorageFileInfo>> ListVersionFilesAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up orphaned files.
    /// </summary>
    Task<StorageCleanupResult> CleanupOrphanedFilesAsync(
        DatasetId datasetId,
        IReadOnlyList<DatasetVersionId> validVersionIds,
        CancellationToken cancellationToken = default);

    #endregion

    #region Path Management

    /// <summary>
    /// Get the storage path for a dataset's versions.
    /// </summary>
    string GetDatasetVersionsPath(DatasetId datasetId);

    /// <summary>
    /// Get the storage path for a specific version.
    /// </summary>
    string GetVersionPath(DatasetId datasetId, DatasetVersionId versionId);

    /// <summary>
    /// Get the storage path for a delta.
    /// </summary>
    string GetDeltaPath(DatasetId datasetId, Guid deltaId);

    /// <summary>
    /// Get the metadata file path for a dataset.
    /// </summary>
    string GetMetadataPath(DatasetId datasetId);

    #endregion
}

/// <summary>
/// Information about a stored file.
/// </summary>
public record StorageFileInfo
{
    public string Path { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public StorageFileType FileType { get; init; }
}

public enum StorageFileType
{
    FullSnapshot,
    Delta,
    Metadata,
    Temp,
    Unknown
}

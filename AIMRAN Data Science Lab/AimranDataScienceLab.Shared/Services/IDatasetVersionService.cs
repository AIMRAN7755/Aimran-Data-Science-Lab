using AIMRAN_Data_Science_Lab.Models.DatasetVersioning;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing dataset versions with snapshot, diff, and rollback capabilities.
/// Provides "Git-like" versioning for datasets.
/// </summary>
public interface IDatasetVersionService
{
    #region Snapshot Operations

    /// <summary>
    /// Create a new snapshot of a dataset.
    /// Automatically chooses between full snapshot and delta based on strategy.
    /// </summary>
    Task<SnapshotResult> CreateSnapshotAsync(
        DatasetSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a full snapshot regardless of delta optimization.
    /// </summary>
    Task<SnapshotResult> CreateFullSnapshotAsync(
        DatasetSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a dataset has changed since last snapshot.
    /// </summary>
    Task<bool> HasChangesAsync(
        DatasetId datasetId,
        string currentPath,
        CancellationToken cancellationToken = default);

    #endregion

    #region Diff Operations

    /// <summary>
    /// Compare two versions of a dataset.
    /// </summary>
    Task<DatasetDiffResult> DiffAsync(
        DatasetVersionId fromVersion,
        DatasetVersionId toVersion,
        DiffOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare current dataset state with a specific version.
    /// </summary>
    Task<DatasetDiffResult> DiffWithCurrentAsync(
        DatasetId datasetId,
        string currentPath,
        DatasetVersionId compareToVersion,
        DiffOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a quick summary of changes without full diff computation.
    /// </summary>
    Task<DiffSummary> GetDiffSummaryAsync(
        DatasetVersionId fromVersion,
        DatasetVersionId toVersion,
        CancellationToken cancellationToken = default);

    #endregion

    #region Rollback Operations

    /// <summary>
    /// Rollback a dataset to a specific version.
    /// Creates a new version representing the rollback state.
    /// </summary>
    Task<RollbackResult> RollbackAsync(
        RollbackRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview what a rollback would change without executing it.
    /// </summary>
    Task<RollbackPreview> PreviewRollbackAsync(
        DatasetId datasetId,
        DatasetVersionId targetVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if rollback to a specific version is possible.
    /// </summary>
    Task<RollbackValidation> ValidateRollbackAsync(
        DatasetId datasetId,
        DatasetVersionId targetVersion,
        CancellationToken cancellationToken = default);

    #endregion

    #region Version History

    /// <summary>
    /// Get the complete version history for a dataset.
    /// </summary>
    Task<VersionHistory> GetVersionHistoryAsync(
        DatasetId datasetId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific version by ID.
    /// </summary>
    Task<DatasetVersion?> GetVersionAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current (latest) version of a dataset.
    /// </summary>
    Task<DatasetVersion?> GetCurrentVersionAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all versions for a dataset.
    /// </summary>
    Task<IReadOnlyList<DatasetVersion>> GetAllVersionsAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search versions by tags or metadata.
    /// </summary>
    Task<IReadOnlyList<DatasetVersion>> SearchVersionsAsync(
        DatasetId datasetId,
        VersionSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    #endregion

    #region Version Management

    /// <summary>
    /// Update metadata for a version.
    /// </summary>
    Task<DatasetVersion> UpdateVersionMetadataAsync(
        DatasetVersionId versionId,
        VersionMetadataUpdate update,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add tags to a version.
    /// </summary>
    Task<DatasetVersion> AddTagsAsync(
        DatasetVersionId versionId,
        IReadOnlyDictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive a version (soft delete).
    /// </summary>
    Task ArchiveVersionAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently delete a version.
    /// </summary>
    Task DeleteVersionAsync(
        DatasetVersionId versionId,
        bool force = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Data Access

    /// <summary>
    /// Export a specific version to a file.
    /// </summary>
    Task<string> ExportVersionAsync(
        DatasetVersionId versionId,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the file path for a version's data.
    /// </summary>
    Task<string> GetVersionDataPathAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify integrity of a version's data.
    /// </summary>
    Task<HashVerificationResult> VerifyVersionIntegrityAsync(
        DatasetVersionId versionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Storage Management

    /// <summary>
    /// Get storage statistics for a dataset's versions.
    /// </summary>
    Task<VersionStorageStats> GetStorageStatsAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consolidate delta chains to optimize storage.
    /// </summary>
    Task<StorageCleanupResult> ConsolidateDeltasAsync(
        DatasetId datasetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old/archived versions.
    /// </summary>
    Task<StorageCleanupResult> CleanupStorageAsync(
        DatasetId datasetId,
        StorageCleanupOptions options,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Options for diff operations.
/// </summary>
public record DiffOptions
{
    public bool IncludeSampleChanges { get; init; } = true;
    public int MaxSampleRows { get; init; } = 100;
    public bool ComputeColumnStats { get; init; } = true;
    public bool DetectRenames { get; init; } = true;
    public double RenameThreshold { get; init; } = 0.8;
    public IReadOnlyList<string>? ColumnsToCompare { get; init; }
    public string? PrimaryKeyColumn { get; init; }
}

/// <summary>
/// Quick summary of differences.
/// </summary>
public record DiffSummary
{
    public DatasetVersionId FromVersionId { get; init; }
    public DatasetVersionId ToVersionId { get; init; }
    public int RowsAdded { get; init; }
    public int RowsRemoved { get; init; }
    public int RowsModified { get; init; }
    public bool HasSchemaChanges { get; init; }
    public double ChangePercentage { get; init; }
    public DiffSeverity Severity { get; init; }
}

/// <summary>
/// Preview of what a rollback would change.
/// </summary>
public record RollbackPreview
{
    public DatasetVersionId CurrentVersionId { get; init; }
    public DatasetVersionId TargetVersionId { get; init; }
    public int VersionsToRevert { get; init; }
    public DatasetDiffResult PredictedChanges { get; init; } = new();
    public RollbackMethod RecommendedMethod { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Validation result for rollback operation.
/// </summary>
public record RollbackValidation
{
    public bool IsValid { get; init; }
    public bool VersionExists { get; init; }
    public bool DataAvailable { get; init; }
    public bool IntegrityVerified { get; init; }
    public IReadOnlyList<string> Issues { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Search criteria for finding versions.
/// </summary>
public record VersionSearchCriteria
{
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public string? CreatedBy { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
    public DatasetVersionType? VersionType { get; init; }
    public DatasetVersionStatus? Status { get; init; }
}

/// <summary>
/// Update request for version metadata.
/// </summary>
public record VersionMetadataUpdate
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
}

/// <summary>
/// Storage statistics for versions.
/// </summary>
public record VersionStorageStats
{
    public DatasetId DatasetId { get; init; }
    public int TotalVersions { get; init; }
    public int FullSnapshots { get; init; }
    public int Deltas { get; init; }
    public long TotalStorageBytes { get; init; }
    public long FullSnapshotBytes { get; init; }
    public long DeltaBytes { get; init; }
    public long StorageSaved { get; init; }
    public double CompressionRatio { get; init; }
    public int MaxDeltaChainLength { get; init; }
    public DateTime OldestVersion { get; init; }
    public DateTime NewestVersion { get; init; }
}

/// <summary>
/// Options for storage cleanup.
/// </summary>
public record StorageCleanupOptions
{
    public bool DeleteArchivedVersions { get; init; }
    public int? KeepLastNVersions { get; init; }
    public DateTime? DeleteOlderThan { get; init; }
    public bool ConsolidateDeltas { get; init; } = true;
    public bool DryRun { get; init; }
}

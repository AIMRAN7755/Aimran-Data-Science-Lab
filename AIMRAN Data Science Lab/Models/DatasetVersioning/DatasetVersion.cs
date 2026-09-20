namespace AIMRAN_Data_Science_Lab.Models.DatasetVersioning;

/// <summary>
/// Strongly-typed identifier for a dataset.
/// </summary>
public readonly record struct DatasetId(Guid Value)
{
    public static DatasetId New() => new(Guid.NewGuid());
    public static DatasetId From(Guid id) => new(id);
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(DatasetId id) => id.Value;
    public static implicit operator DatasetId(Guid id) => new(id);
}

/// <summary>
/// Strongly-typed identifier for a dataset version.
/// </summary>
public readonly record struct DatasetVersionId(Guid Value)
{
    public static DatasetVersionId New() => new(Guid.NewGuid());
    public static DatasetVersionId From(Guid id) => new(id);
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(DatasetVersionId id) => id.Value;
    public static implicit operator DatasetVersionId(Guid id) => new(id);
}

/// <summary>
/// Represents a single version of a dataset.
/// </summary>
public record DatasetVersion
{
    public DatasetVersionId Id { get; init; } = DatasetVersionId.New();
    public DatasetId DatasetId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int VersionNumber { get; init; } = 1;
    public DatasetVersionId? ParentVersionId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string CreatedBy { get; init; } = "system";
    public string StoragePointer { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DatasetVersionType VersionType { get; init; } = DatasetVersionType.FullSnapshot;
    public DatasetVersionStatus Status { get; init; } = DatasetVersionStatus.Active;
    public VersionMetadata Metadata { get; init; } = new();
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Metadata associated with a dataset version.
/// </summary>
public record VersionMetadata
{
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public IReadOnlyList<string> ColumnNames { get; init; } = [];
    public IReadOnlyDictionary<string, string> ColumnTypes { get; init; } = new Dictionary<string, string>();
    public double? QualityScore { get; init; }
    public string? SourcePipeline { get; init; }
    public IReadOnlyList<Guid> UsedInExperiments { get; init; } = [];
    public string? CleaningSessionId { get; init; }
    public VersionChangeStats? ChangeStats { get; init; }
}

/// <summary>
/// Statistics about changes in this version.
/// </summary>
public record VersionChangeStats
{
    public int RowsAdded { get; init; }
    public int RowsRemoved { get; init; }
    public int RowsModified { get; init; }
    public int ColumnsAdded { get; init; }
    public int ColumnsRemoved { get; init; }
    public double ChangePercentage { get; init; }
}

/// <summary>
/// Request to create a new dataset snapshot.
/// </summary>
public record DatasetSnapshotRequest
{
    public DatasetId DatasetId { get; init; }
    public string DatasetPath { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string CreatedBy { get; init; } = "system";
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public SnapshotStrategy Strategy { get; init; } = SnapshotStrategy.Auto;
    public bool Force { get; init; }
}

/// <summary>
/// Result of creating a snapshot.
/// </summary>
public record SnapshotResult
{
    public bool Success { get; init; }
    public DatasetVersion? Version { get; init; }
    public string? ErrorMessage { get; init; }
    public SnapshotStrategy UsedStrategy { get; init; }
    public TimeSpan Duration { get; init; }
    public long StorageSaved { get; init; }
}

/// <summary>
/// Request to rollback to a specific version.
/// </summary>
public record RollbackRequest
{
    public DatasetId DatasetId { get; init; }
    public DatasetVersionId TargetVersionId { get; init; }
    public string? Reason { get; init; }
    public string RequestedBy { get; init; } = "system";
    public bool CreateBackup { get; init; } = true;
}

/// <summary>
/// Result of a rollback operation.
/// </summary>
public record RollbackResult
{
    public bool Success { get; init; }
    public DatasetVersion? NewVersion { get; init; }
    public DatasetVersion? BackupVersion { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public RollbackMethod MethodUsed { get; init; }
}

/// <summary>
/// Version history with lineage information.
/// </summary>
public record VersionHistory
{
    public DatasetId DatasetId { get; init; }
    public string DatasetName { get; init; } = string.Empty;
    public int TotalVersions { get; init; }
    public DatasetVersion? CurrentVersion { get; init; }
    public IReadOnlyList<DatasetVersion> Versions { get; init; } = [];
    public IReadOnlyList<VersionLineageNode> LineageGraph { get; init; } = [];
    public long TotalStorageUsed { get; init; }
    public long StorageSavedByDeltas { get; init; }
}

/// <summary>
/// Node in the version lineage graph.
/// </summary>
public record VersionLineageNode
{
    public DatasetVersionId VersionId { get; init; }
    public int VersionNumber { get; init; }
    public DatasetVersionId? ParentId { get; init; }
    public IReadOnlyList<DatasetVersionId> ChildIds { get; init; } = [];
    public DatasetVersionType Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Label { get; init; } = string.Empty;
}

public enum DatasetVersionType
{
    FullSnapshot,
    Delta,
    Rollback,
    Import,
    CleaningResult
}

public enum DatasetVersionStatus
{
    Active,
    Archived,
    Deleted,
    Processing,
    Failed
}

public enum SnapshotStrategy
{
    Auto,
    FullSnapshot,
    DeltaOnly,
    Hybrid
}

public enum RollbackMethod
{
    DirectRestore,
    DeltaChainReplay,
    FullRebuild
}

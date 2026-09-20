namespace AIMRAN_Data_Science_Lab.Models.DatasetVersioning;

/// <summary>
/// Represents a delta (incremental change) between versions.
/// </summary>
public record DatasetDelta
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DatasetVersionId BaseVersionId { get; init; }
    public DatasetVersionId TargetVersionId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string StoragePath { get; init; } = string.Empty;
    public long DeltaSizeBytes { get; init; }
    public long OriginalSizeBytes { get; init; }
    public double CompressionRatio => OriginalSizeBytes > 0 ? (double)DeltaSizeBytes / OriginalSizeBytes : 1.0;
    public string Hash { get; init; } = string.Empty;
    public DeltaType Type { get; init; }
    public DeltaOperations Operations { get; init; } = new();
}

/// <summary>
/// Operations contained in a delta.
/// </summary>
public record DeltaOperations
{
    public IReadOnlyList<DeltaRowOperation> RowOperations { get; init; } = [];
    public IReadOnlyList<DeltaSchemaOperation> SchemaOperations { get; init; } = [];
    public int TotalOperations => RowOperations.Count + SchemaOperations.Count;
}

/// <summary>
/// A row-level operation in the delta.
/// </summary>
public record DeltaRowOperation
{
    public DeltaOperationType Type { get; init; }
    public int RowIndex { get; init; }
    public string? RowKey { get; init; }
    public IReadOnlyDictionary<string, string?>? NewValues { get; init; }
    public IReadOnlyDictionary<string, string?>? OldValues { get; init; }
    public IReadOnlyList<string>? ModifiedColumns { get; init; }
}

/// <summary>
/// A schema-level operation in the delta.
/// </summary>
public record DeltaSchemaOperation
{
    public DeltaSchemaOperationType Type { get; init; }
    public string? ColumnName { get; init; }
    public string? NewColumnName { get; init; }
    public string? DataType { get; init; }
    public string? NewDataType { get; init; }
    public int? ColumnIndex { get; init; }
    public int? NewColumnIndex { get; init; }
    public string? DefaultValue { get; init; }
}

/// <summary>
/// Delta chain for version reconstruction.
/// </summary>
public record DeltaChain
{
    public DatasetVersionId BaseSnapshotId { get; init; }
    public IReadOnlyList<DatasetDelta> Deltas { get; init; } = [];
    public int ChainLength => Deltas.Count;
    public long TotalDeltaSize => Deltas.Sum(d => d.DeltaSizeBytes);
    public bool NeedsConsolidation => ChainLength > 10;
}

/// <summary>
/// Storage configuration for versioning.
/// </summary>
public record VersionStorageConfig
{
    public string BasePath { get; init; } = string.Empty;
    public long DeltaThresholdBytes { get; init; } = 1024 * 1024; // 1MB
    public double DeltaThresholdRatio { get; init; } = 0.3; // 30%
    public int MaxDeltaChainLength { get; init; } = 10;
    public bool CompressDeltas { get; init; } = true;
    public bool VerifyHashes { get; init; } = true;
    public StorageProvider Provider { get; init; } = StorageProvider.Local;
}

/// <summary>
/// Hash verification result.
/// </summary>
public record HashVerificationResult
{
    public bool IsValid { get; init; }
    public string ExpectedHash { get; init; } = string.Empty;
    public string ActualHash { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Version rebuild result.
/// </summary>
public record VersionRebuildResult
{
    public bool Success { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public int DeltasApplied { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Storage cleanup result.
/// </summary>
public record StorageCleanupResult
{
    public int VersionsRemoved { get; init; }
    public int DeltasConsolidated { get; init; }
    public long BytesReclaimed { get; init; }
    public TimeSpan Duration { get; init; }
}

public enum DeltaType
{
    RowLevel,
    ColumnLevel,
    Mixed,
    Compressed
}

public enum DeltaOperationType
{
    Insert,
    Delete,
    Update,
    Move
}

public enum DeltaSchemaOperationType
{
    AddColumn,
    RemoveColumn,
    RenameColumn,
    ChangeType,
    ReorderColumn,
    SetDefault
}

public enum StorageProvider
{
    Local,
    AzureBlob,
    S3,
    MinIO
}

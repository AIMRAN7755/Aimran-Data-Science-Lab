using AIMRAN_Data_Science_Lab.Models.DatasetVersioning;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Engine for computing, applying, and managing dataset deltas.
/// Provides efficient incremental change tracking and reconstruction.
/// </summary>
public interface IDeltaEngine
{
    #region Delta Computation

    /// <summary>
    /// Compute the delta between two dataset files.
    /// </summary>
    Task<DeltaComputeResult> ComputeDeltaAsync(
        string baseFilePath,
        string targetFilePath,
        DeltaComputeOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute delta from in-memory data.
    /// </summary>
    Task<DeltaComputeResult> ComputeDeltaFromDataAsync(
        IReadOnlyList<IReadOnlyList<string>> baseRows,
        IReadOnlyList<IReadOnlyList<string>> targetRows,
        IReadOnlyList<string> columns,
        DeltaComputeOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine if delta storage is more efficient than full snapshot.
    /// </summary>
    Task<DeltaRecommendation> ShouldUseDeltaAsync(
        string baseFilePath,
        string targetFilePath,
        VersionStorageConfig config,
        CancellationToken cancellationToken = default);

    #endregion

    #region Delta Application

    /// <summary>
    /// Apply a delta to a base dataset to reconstruct target version.
    /// </summary>
    Task<DeltaApplyResult> ApplyDeltaAsync(
        string baseFilePath,
        DatasetDelta delta,
        byte[] deltaData,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply multiple deltas in sequence to reconstruct a version.
    /// </summary>
    Task<DeltaApplyResult> ApplyDeltaChainAsync(
        string baseFilePath,
        DeltaChain chain,
        Func<DatasetDelta, Task<byte[]>> deltaLoader,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply delta in reverse (for rollback from newer to older).
    /// </summary>
    Task<DeltaApplyResult> ApplyReverseDeltaAsync(
        string currentFilePath,
        DatasetDelta delta,
        byte[] deltaData,
        string outputPath,
        CancellationToken cancellationToken = default);

    #endregion

    #region Delta Serialization

    /// <summary>
    /// Serialize a delta to binary format.
    /// </summary>
    byte[] SerializeDelta(DatasetDelta delta);

    /// <summary>
    /// Deserialize a delta from binary format.
    /// </summary>
    DatasetDelta DeserializeDelta(byte[] data);

    /// <summary>
    /// Compress delta data.
    /// </summary>
    byte[] CompressDelta(byte[] deltaData);

    /// <summary>
    /// Decompress delta data.
    /// </summary>
    byte[] DecompressDelta(byte[] compressedData);

    #endregion

    #region Delta Chain Management

    /// <summary>
    /// Build a delta chain from base snapshot to target version.
    /// </summary>
    Task<DeltaChain> BuildDeltaChainAsync(
        DatasetVersionId baseSnapshotId,
        DatasetVersionId targetVersionId,
        Func<DatasetVersionId, Task<DatasetVersion>> versionLoader,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consolidate a delta chain into a single delta.
    /// </summary>
    Task<DeltaConsolidateResult> ConsolidateDeltaChainAsync(
        DeltaChain chain,
        string baseFilePath,
        Func<DatasetDelta, Task<byte[]>> deltaLoader,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimize a delta chain by merging consecutive small deltas.
    /// </summary>
    Task<DeltaOptimizeResult> OptimizeDeltaChainAsync(
        DeltaChain chain,
        Func<DatasetDelta, Task<byte[]>> deltaLoader,
        DeltaOptimizeOptions options,
        CancellationToken cancellationToken = default);

    #endregion

    #region Diff Operations

    /// <summary>
    /// Compare two datasets and generate a detailed diff.
    /// </summary>
    Task<DatasetDiffResult> DiffDatasetsAsync(
        string fromFilePath,
        string toFilePath,
        DiffOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate diff from computed delta.
    /// </summary>
    DatasetDiffResult DeltaToDiff(
        DatasetDelta delta,
        int fromRowCount,
        int toRowCount);

    #endregion
}

/// <summary>
/// Result of delta computation.
/// </summary>
public record DeltaComputeResult
{
    public bool Success { get; init; }
    public DatasetDelta? Delta { get; init; }
    public byte[]? DeltaData { get; init; }
    public long OriginalSize { get; init; }
    public long DeltaSize { get; init; }
    public double CompressionRatio => OriginalSize > 0 ? (double)DeltaSize / OriginalSize : 1.0;
    public TimeSpan ComputeDuration { get; init; }
    public string? ErrorMessage { get; init; }
    public DeltaComputeStats Stats { get; init; } = new();
}

/// <summary>
/// Statistics from delta computation.
/// </summary>
public record DeltaComputeStats
{
    public int RowsCompared { get; init; }
    public int RowsMatched { get; init; }
    public int RowsAdded { get; init; }
    public int RowsRemoved { get; init; }
    public int RowsModified { get; init; }
    public int OperationsGenerated { get; init; }
}

/// <summary>
/// Options for delta computation.
/// </summary>
public record DeltaComputeOptions
{
    public string? PrimaryKeyColumn { get; init; }
    public bool DetectMoves { get; init; } = false;
    public bool Compress { get; init; } = true;
    public int MaxOperations { get; init; } = 100000;
    public double SimilarityThreshold { get; init; } = 0.9;
}

/// <summary>
/// Recommendation for delta vs full snapshot.
/// </summary>
public record DeltaRecommendation
{
    public bool UseDelta { get; init; }
    public string Reason { get; init; } = string.Empty;
    public double EstimatedDeltaSize { get; init; }
    public double EstimatedFullSize { get; init; }
    public double EstimatedSavings { get; init; }
    public double ChangePercentage { get; init; }
}

/// <summary>
/// Result of applying a delta.
/// </summary>
public record DeltaApplyResult
{
    public bool Success { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public int OperationsApplied { get; init; }
    public int RowsInResult { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of consolidating a delta chain.
/// </summary>
public record DeltaConsolidateResult
{
    public bool Success { get; init; }
    public DatasetDelta? ConsolidatedDelta { get; init; }
    public byte[]? DeltaData { get; init; }
    public int DeltasConsolidated { get; init; }
    public long OriginalTotalSize { get; init; }
    public long ConsolidatedSize { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Options for delta chain optimization.
/// </summary>
public record DeltaOptimizeOptions
{
    public int MaxChainLength { get; init; } = 10;
    public long MinDeltaSizeToMerge { get; init; } = 1024;
    public bool PreserveKeyVersions { get; init; } = true;
}

/// <summary>
/// Result of delta chain optimization.
/// </summary>
public record DeltaOptimizeResult
{
    public bool Success { get; init; }
    public int OriginalChainLength { get; init; }
    public int OptimizedChainLength { get; init; }
    public long StorageSaved { get; init; }
    public IReadOnlyList<DatasetDelta> OptimizedDeltas { get; init; } = [];
    public string? ErrorMessage { get; init; }
}

namespace AimranDataScienceLab.Gateway;

/// <summary>
/// Client for the Rust high-performance Resource Engine.
/// Handles resource monitoring, delta computation, and heavy I/O operations.
/// </summary>
public interface IRustEngineClient
{
    #region Health

    /// <summary>
    /// Check if the Rust engine is reachable and healthy.
    /// </summary>
    Task<EngineHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Resource Monitoring

    /// <summary>
    /// Get current system resource metrics from the Rust engine.
    /// </summary>
    Task<RustResourceSnapshot> GetResourceSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream real-time resource metrics.
    /// </summary>
    IAsyncEnumerable<RustResourceSnapshot> StreamResourcesAsync(
        TimeSpan interval,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect GPU capabilities via the Rust engine.
    /// </summary>
    Task<RustGpuInfo?> DetectGpuAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Delta Computation

    /// <summary>
    /// Compute binary delta between two dataset files using Rust's high-performance engine.
    /// </summary>
    Task<RustDeltaResult> ComputeDeltaAsync(
        string baseFilePath,
        string targetFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply a delta to reconstruct a target file.
    /// </summary>
    Task<RustApplyDeltaResult> ApplyDeltaAsync(
        string baseFilePath,
        byte[] deltaData,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute file hash using the Rust engine (SHA-256).
    /// </summary>
    Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    #endregion

    #region File I/O

    /// <summary>
    /// Parse a CSV file with high performance using Rust.
    /// </summary>
    Task<RustCsvParseResult> ParseCsvAsync(
        string filePath,
        int maxRows = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert a file between formats using the Rust engine.
    /// </summary>
    Task<string> ConvertFileAsync(
        string sourcePath,
        string targetFormat,
        string outputPath,
        CancellationToken cancellationToken = default);

    #endregion
}

#region Rust Engine DTOs

public sealed record RustResourceSnapshot
{
    public double CpuUsagePercent { get; init; }
    public int CpuCoreCount { get; init; }
    public double CpuFrequencyMhz { get; init; }
    public long MemoryTotalBytes { get; init; }
    public long MemoryUsedBytes { get; init; }
    public long MemoryAvailableBytes { get; init; }
    public long DiskTotalBytes { get; init; }
    public long DiskUsedBytes { get; init; }
    public long DiskFreeBytes { get; init; }
    public RustGpuInfo? Gpu { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed record RustGpuInfo
{
    public required string Name { get; init; }
    public double UsagePercent { get; init; }
    public long MemoryTotalBytes { get; init; }
    public long MemoryUsedBytes { get; init; }
    public double TemperatureCelsius { get; init; }
    public bool CudaAvailable { get; init; }
}

public sealed record RustDeltaResult
{
    public required byte[] DeltaData { get; init; }
    public long OriginalSizeBytes { get; init; }
    public long DeltaSizeBytes { get; init; }
    public double CompressionRatio { get; init; }
    public required string BaseHash { get; init; }
    public required string TargetHash { get; init; }
    public TimeSpan ComputeDuration { get; init; }
}

public sealed record RustApplyDeltaResult
{
    public required string OutputPath { get; init; }
    public required string ResultHash { get; init; }
    public long OutputSizeBytes { get; init; }
    public TimeSpan ApplyDuration { get; init; }
}

public sealed record RustCsvParseResult
{
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
    public int TotalRowCount { get; init; }
    public TimeSpan ParseDuration { get; init; }
}

#endregion

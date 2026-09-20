namespace AIMRAN_Data_Science_Lab.Models;

/// <summary>
/// Represents system resource metrics for monitoring.
/// </summary>
public record ResourceMetrics
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public CpuMetrics Cpu { get; init; } = new();
    public MemoryMetrics Memory { get; init; } = new();
    public GpuMetrics? Gpu { get; init; }
    public DiskMetrics Disk { get; init; } = new();
}

public record CpuMetrics
{
    public double UsagePercent { get; init; }
    public int CoreCount { get; init; }
    public double FrequencyMhz { get; init; }
}

public record MemoryMetrics
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long AvailableBytes { get; init; }
    public double UsagePercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
}

public record GpuMetrics
{
    public required string Name { get; init; }
    public double UsagePercent { get; init; }
    public long MemoryTotalBytes { get; init; }
    public long MemoryUsedBytes { get; init; }
    public double TemperatureCelsius { get; init; }
    public bool IsAvailable { get; init; }
}

public record DiskMetrics
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long FreeBytes { get; init; }
    public double UsagePercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
}

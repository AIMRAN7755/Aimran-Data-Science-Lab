using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for monitoring system resources.
/// </summary>
public interface IResourceMonitorService
{
    Task<ResourceMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<ResourceMetrics> StreamMetricsAsync(TimeSpan interval, CancellationToken cancellationToken = default);
    Task<bool> IsGpuAvailableAsync(CancellationToken cancellationToken = default);
    Task<GpuMetrics?> GetGpuMetricsAsync(CancellationToken cancellationToken = default);
}

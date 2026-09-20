using System.Diagnostics;
using System.Runtime.CompilerServices;
using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation of resource monitoring using system APIs.
/// </summary>
internal sealed class LocalResourceMonitorService : IResourceMonitorService
{
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    public Task<ResourceMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metrics = new ResourceMetrics
        {
            Timestamp = DateTime.UtcNow,
            Cpu = GetCpuMetrics(),
            Memory = GetMemoryMetrics(),
            Disk = GetDiskMetrics(),
            Gpu = null // GPU metrics require platform-specific implementation
        };

        return Task.FromResult(metrics);
    }

    public async IAsyncEnumerable<ResourceMetrics> StreamMetricsAsync(
        TimeSpan interval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return await GetCurrentMetricsAsync(cancellationToken);
            await Task.Delay(interval, cancellationToken);
        }
    }

    public Task<bool> IsGpuAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder - real implementation would check for CUDA/DirectML availability
        return Task.FromResult(false);
    }

    public Task<GpuMetrics?> GetGpuMetricsAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder - real implementation would use NVML or DirectML APIs
        return Task.FromResult<GpuMetrics?>(null);
    }

    private CpuMetrics GetCpuMetrics()
    {
        return new CpuMetrics
        {
            CoreCount = Environment.ProcessorCount,
            UsagePercent = _currentProcess.TotalProcessorTime.TotalMilliseconds /
                          (Environment.ProcessorCount * Environment.TickCount) * 100,
            FrequencyMhz = 0 // Would require platform-specific APIs
        };
    }

    private MemoryMetrics GetMemoryMetrics()
    {
        var gcMemory = GC.GetGCMemoryInfo();
        return new MemoryMetrics
        {
            TotalBytes = gcMemory.TotalAvailableMemoryBytes,
            UsedBytes = _currentProcess.WorkingSet64,
            AvailableBytes = gcMemory.TotalAvailableMemoryBytes - _currentProcess.WorkingSet64
        };
    }

    private static DiskMetrics GetDiskMetrics()
    {
        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);
        if (drive is null)
        {
            return new DiskMetrics();
        }

        return new DiskMetrics
        {
            TotalBytes = drive.TotalSize,
            FreeBytes = drive.AvailableFreeSpace,
            UsedBytes = drive.TotalSize - drive.AvailableFreeSpace
        };
    }
}

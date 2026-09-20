using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AimranDataScienceLab.Engine;

/// <summary>
/// Manages external engine processes (Python, Rust, R) for local desktop deployments.
/// Handles process lifecycle (start/stop), health checks, port management, and log streaming.
/// </summary>
public interface IEngineManager
{
    /// <summary>Get status of all engines</summary>
    Task<EngineStatusSnapshot> GetStatusAsync();

    /// <summary>Start a specific engine</summary>
    Task<bool> StartEngineAsync(ExternalEngine engine, CancellationToken cancellationToken = default);

    /// <summary>Stop a specific engine</summary>
    Task<bool> StopEngineAsync(ExternalEngine engine, CancellationToken cancellationToken = default);

    /// <summary>Restart an engine</summary>
    Task<bool> RestartEngineAsync(ExternalEngine engine, CancellationToken cancellationToken = default);

    /// <summary>Subscribe to engine status changes</summary>
    IAsyncEnumerable<EngineStatusChange> WatchEngineStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Overload for interface compatibility</summary>
    IAsyncEnumerable<EngineStatusChange> WatchEngineStatusAsync(System.Collections.Generic.IAsyncEnumerable<object>? filter, CancellationToken cancellationToken);

    /// <summary>Get recent log entries for an engine</summary>
    Task<IReadOnlyList<string>> GetEngineLogsAsync(ExternalEngine engine, int lineCount = 50);

    /// <summary>Stream logs in real-time</summary>
    IAsyncEnumerable<string> StreamLogsAsync(ExternalEngine engine, CancellationToken cancellationToken = default);

    /// <summary>Configure engine settings</summary>
    Task ConfigureEngineAsync(ExternalEngine engine, EngineConfiguration config);

    /// <summary>Check if engine is available (binary exists, dependencies installed)</summary>
    Task<bool> IsEngineAvailableAsync(ExternalEngine engine);

    /// <summary>Auto-start enabled engines on app startup</summary>
    Task AutoStartEnginesAsync(CancellationToken cancellationToken = default);

    /// <summary>Gracefully shutdown all running engines</summary>
    Task ShutdownAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>External engine types</summary>
public enum ExternalEngine
{
    Python,  // FastAPI on port 8100
    Rust,    // Actix-Web on port 8200
    R        // Plumber on port 8300
}

/// <summary>Engine process status</summary>
public enum EngineStatus
{
    Unknown,
    Running,
    Stopped,
    Error,
    Starting,
    Stopping,
    Unavailable  // Binary not found or dependencies missing
}

/// <summary>Status of all engines at a point in time</summary>
public record EngineStatusSnapshot(
    EngineProcessStatus Python,
    EngineProcessStatus Rust,
    EngineProcessStatus R,
    DateTime Timestamp
);

/// <summary>Status of a single engine</summary>
public record EngineProcessStatus(
    ExternalEngine Engine,
    EngineStatus Status,
    int? ProcessId,
    int Port,
    DateTime? LastStateChange,
    string? ErrorMessage = null,
    EngineMetrics? Metrics = null
);

/// <summary>Engine metrics snapshot</summary>
public record EngineMetrics(
    int MemoryMB,
    double CpuPercent,
    int ThreadCount,
    TimeSpan Uptime
);

/// <summary>Status change event</summary>
public record EngineStatusChange(
    ExternalEngine Engine,
    EngineStatus OldStatus,
    EngineStatus NewStatus,
    DateTime Timestamp
);

/// <summary>Engine configuration</summary>
public record EngineConfiguration(
    int Port,
    string? WorkingDirectory = null,
    Dictionary<string, string>? EnvironmentVariables = null,
    bool AutoStart = false,
    int? HealthCheckIntervalMs = 5000,
    int? ProcessTimeoutMs = 30000
);

/// <summary>Implementation of IEngineManager</summary>
public class EngineManager : IEngineManager, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<ExternalEngine, EngineProcessStatus> _engineStatus;
    private readonly Dictionary<ExternalEngine, Process?> _runningProcesses;
    private readonly Dictionary<ExternalEngine, EngineConfiguration> _engineConfigs;
    private readonly Dictionary<ExternalEngine, List<string>> _engineLogs;
    private readonly CancellationTokenSource _internalCts;
    private readonly object _statusLock = new();

    // Channel for status change events
    private System.Collections.Concurrent.ConcurrentQueue<EngineStatusChange> _statusChangeQueue;

    public EngineManager(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(5);

        _engineStatus = new();
        _runningProcesses = new();
        _engineConfigs = new();
        _engineLogs = new();
        _internalCts = new CancellationTokenSource();
        _statusChangeQueue = new();

        InitializeDefaultConfigurations();
        InitializeStatusMap();
    }

    private void InitializeDefaultConfigurations()
    {
        _engineConfigs[ExternalEngine.Python] = new EngineConfiguration(
            Port: 8100,
            AutoStart: false,
            HealthCheckIntervalMs: 5000
        );

        _engineConfigs[ExternalEngine.Rust] = new EngineConfiguration(
            Port: 8200,
            AutoStart: false,
            HealthCheckIntervalMs: 5000
        );

        _engineConfigs[ExternalEngine.R] = new EngineConfiguration(
            Port: 8300,
            AutoStart: false,
            HealthCheckIntervalMs: 10000
        );
    }

    private void InitializeStatusMap()
    {
        lock (_statusLock)
        {
            foreach (var engine in Enum.GetValues<ExternalEngine>())
            {
                _engineStatus[engine] = new EngineProcessStatus(
                    Engine: engine,
                    Status: EngineStatus.Unknown,
                    ProcessId: null,
                    Port: _engineConfigs[engine].Port,
                    LastStateChange: null
                );
                _runningProcesses[engine] = null;
                _engineLogs[engine] = new List<string>();
            }
        }
    }

    public async Task<EngineStatusSnapshot> GetStatusAsync()
    {
        lock (_statusLock)
        {
            return new EngineStatusSnapshot(
                Python: _engineStatus[ExternalEngine.Python],
                Rust: _engineStatus[ExternalEngine.Rust],
                R: _engineStatus[ExternalEngine.R],
                Timestamp: DateTime.UtcNow
            );
        }
    }

    public async Task<bool> StartEngineAsync(ExternalEngine engine, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_statusLock)
            {
                if (_engineStatus[engine].Status == EngineStatus.Running)
                    return true;

                UpdateStatus(engine, EngineStatus.Starting, "Starting engine...");
            }

            var config = _engineConfigs[engine];
            var process = CreateEngineProcess(engine, config);

            if (process == null)
            {
                UpdateStatus(engine, EngineStatus.Unavailable, "Engine binary not found or dependencies missing.");
                return false;
            }

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lock (_statusLock)
                    {
                        _engineLogs[engine].Add($"[{DateTime.Now:HH:mm:ss}] {e.Data}");
                        if (_engineLogs[engine].Count > 1000)
                            _engineLogs[engine].RemoveAt(0);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();

            lock (_statusLock)
            {
                _runningProcesses[engine] = process;
                UpdateStatus(engine, EngineStatus.Running, null, process.Id);
            }

            // Wait briefly for process to stabilize, then health check
            await Task.Delay(1000, cancellationToken);
            var isHealthy = await HealthCheckAsync(engine);

            if (!isHealthy)
            {
                await StopEngineAsync(engine, cancellationToken);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            UpdateStatus(engine, EngineStatus.Error, $"Failed to start: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopEngineAsync(ExternalEngine engine, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_statusLock)
            {
                if (_engineStatus[engine].Status == EngineStatus.Stopped ||
                    _engineStatus[engine].Status == EngineStatus.Unknown)
                    return true;

                UpdateStatus(engine, EngineStatus.Stopping, "Stopping engine...");
            }

            Process? process = null;
            lock (_statusLock)
            {
                process = _runningProcesses[engine];
            }

            if (process != null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await Task.Run(() => process.WaitForExit(5000), cancellationToken);
            }

            lock (_statusLock)
            {
                _runningProcesses[engine] = null;
                UpdateStatus(engine, EngineStatus.Stopped, "Engine stopped");
            }

            return true;
        }
        catch (Exception ex)
        {
            UpdateStatus(engine, EngineStatus.Error, $"Failed to stop: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestartEngineAsync(ExternalEngine engine, CancellationToken cancellationToken = default)
    {
        await StopEngineAsync(engine, cancellationToken);
        await Task.Delay(500, cancellationToken);
        return await StartEngineAsync(engine, cancellationToken);
    }

    public async IAsyncEnumerable<EngineStatusChange> WatchEngineStatusAsync(
        System.Collections.Generic.IAsyncEnumerable<object>? filter = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while (_statusChangeQueue.TryDequeue(out var change))
            {
                yield return change;
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    // For direct parameter-less call
    public IAsyncEnumerable<EngineStatusChange> WatchEngineStatusAsync(CancellationToken cancellationToken = default)
        => WatchEngineStatusAsync(null, cancellationToken);

    public async Task<IReadOnlyList<string>> GetEngineLogsAsync(ExternalEngine engine, int lineCount = 50)
    {
        lock (_statusLock)
        {
            var logs = _engineLogs[engine];
            return logs.Skip(Math.Max(0, logs.Count - lineCount)).ToList().AsReadOnly();
        }
    }

    public async IAsyncEnumerable<string> StreamLogsAsync(ExternalEngine engine, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int lastCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            lock (_statusLock)
            {
                var logs = _engineLogs[engine];
                for (int i = lastCount; i < logs.Count; i++)
                {
                    yield return logs[i];
                }
                lastCount = logs.Count;
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    public async Task ConfigureEngineAsync(ExternalEngine engine, EngineConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        lock (_statusLock)
        {
            _engineConfigs[engine] = config;
        }
    }

    public async Task<bool> IsEngineAvailableAsync(ExternalEngine engine)
    {
        return engine switch
        {
            ExternalEngine.Python => Directory.Exists(GetEnginePath(engine)) || HasPythonInPath(),
            ExternalEngine.Rust => File.Exists(GetEngineExePath(engine)),
            ExternalEngine.R => HasRInPath(),
            _ => false
        };
    }

    public async Task AutoStartEnginesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var engine in Enum.GetValues<ExternalEngine>())
        {
            if (await IsEngineAvailableAsync(engine) && _engineConfigs[engine].AutoStart)
            {
                await StartEngineAsync(engine, cancellationToken);
            }
        }
    }

    public async Task ShutdownAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var engine in Enum.GetValues<ExternalEngine>())
        {
            await StopEngineAsync(engine, cancellationToken);
        }
    }

    // ── Private Helpers ──

    private Process? CreateEngineProcess(ExternalEngine engine, EngineConfiguration config)
    {
        return engine switch
        {
            ExternalEngine.Python => CreatePythonProcess(config),
            ExternalEngine.Rust => CreateRustProcess(config),
            ExternalEngine.R => CreateRProcess(config),
            _ => null
        };
    }

    private Process? CreatePythonProcess(EngineConfiguration config)
    {
        var pythonPath = GetEnginePath(ExternalEngine.Python);
        if (!Directory.Exists(pythonPath))
            return null;

        var pythonExe = Path.Combine(pythonPath, "python.exe");
        if (!File.Exists(pythonExe))
            pythonExe = "python";  // Assume in PATH

        var mainPy = Path.Combine(pythonPath, "main.py");

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = mainPy,
            WorkingDirectory = config.WorkingDirectory ?? pythonPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (config.EnvironmentVariables != null)
        {
            foreach (var (key, value) in config.EnvironmentVariables)
            {
                psi.Environment[key] = value;
            }
        }

        return new Process { StartInfo = psi };
    }

    private Process? CreateRustProcess(EngineConfiguration config)
    {
        var exePath = GetEngineExePath(ExternalEngine.Rust);
        if (!File.Exists(exePath))
            return null;

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = config.WorkingDirectory ?? Path.GetDirectoryName(exePath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (config.EnvironmentVariables != null)
        {
            foreach (var (key, value) in config.EnvironmentVariables)
            {
                psi.Environment[key] = value;
            }
        }

        return new Process { StartInfo = psi };
    }

    private Process? CreateRProcess(EngineConfiguration config)
    {
        // Try Rscript in PATH first
        var psi = new ProcessStartInfo
        {
            FileName = "Rscript",
            Arguments = Path.Combine(GetEnginePath(ExternalEngine.R), "plumber_api.R"),
            WorkingDirectory = config.WorkingDirectory ?? GetEnginePath(ExternalEngine.R),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (config.EnvironmentVariables != null)
        {
            foreach (var (key, value) in config.EnvironmentVariables)
            {
                psi.Environment[key] = value;
            }
        }

        return new Process { StartInfo = psi };
    }

    private async Task<bool> HealthCheckAsync(ExternalEngine engine)
    {
        var port = _engineConfigs[engine].Port;
        var url = $"http://localhost:{port}/health";

        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateStatus(ExternalEngine engine, EngineStatus newStatus, string? errorMessage = null, int? processId = null)
    {
        var oldStatus = _engineStatus[engine].Status;
        var now = DateTime.UtcNow;

        _engineStatus[engine] = new EngineProcessStatus(
            Engine: engine,
            Status: newStatus,
            ProcessId: processId ?? _engineStatus[engine].ProcessId,
            Port: _engineConfigs[engine].Port,
            LastStateChange: now,
            ErrorMessage: errorMessage
        );

        if (oldStatus != newStatus)
        {
            _statusChangeQueue.Enqueue(new EngineStatusChange(engine, oldStatus, newStatus, now));
        }
    }

    private string GetEnginePath(ExternalEngine engine)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AIMRAN Lab",
            "Engines"
        );

        return engine switch
        {
            ExternalEngine.Python => Path.Combine(appDataPath, "python"),
            ExternalEngine.Rust => Path.Combine(appDataPath, "rust"),
            ExternalEngine.R => Path.Combine(appDataPath, "r"),
            _ => appDataPath
        };
    }

    private string GetEngineExePath(ExternalEngine engine)
    {
        var path = GetEnginePath(engine);
        return engine switch
        {
            ExternalEngine.Rust => Path.Combine(path, "rust-engine.exe"),
            _ => path
        };
    }

    private bool HasPythonInPath()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(1000);
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    private bool HasRInPath()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "Rscript",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(1000);
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    public void Dispose()
    {
        _internalCts?.Cancel();
        _internalCts?.Dispose();

        lock (_statusLock)
        {
            foreach (var process in _runningProcesses.Values.OfType<Process>())
            {
                if (!process.HasExited)
                {
                    try { process.Kill(true); } catch { }
                }
                process.Dispose();
            }
        }
    }
}

// Polyfill for IAsyncEnumerable parameter issue
public static class EngineManagerExtensions
{
    public static IAsyncEnumerable<EngineStatusChange> WatchEngineStatusAsync(
        this IEngineManager manager,
        CancellationToken cancellationToken = default)
        => manager.WatchEngineStatusAsync(null, cancellationToken);
}

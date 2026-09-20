using System.Collections.Concurrent;
using System.Diagnostics;
using AimranDataScienceLab.Gateway.Configuration;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Gateway.Clients;

/// <summary>
/// Manages external engine processes (Python FastAPI, Rust binary).
/// Handles launching, health monitoring, automatic restart on crash, and log capture.
/// </summary>
public sealed class EngineProcessManager : IDisposable
{
    private readonly GatewayOptions _options;
    private readonly ILogger<EngineProcessManager> _logger;
    private readonly ConcurrentDictionary<string, ManagedProcess> _processes = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<EngineLogEntry>> _logBuffers = new();
    private CancellationTokenSource? _watchdogCts;
    private Task? _watchdogTask;
    private bool _disposed;

    private const int MaxLogEntries = 500;
    private const string PythonEngineKey = "python";
    private const string RustEngineKey = "rust";

    public EngineProcessManager(
        Microsoft.Extensions.Options.IOptions<GatewayOptions> options,
        ILogger<EngineProcessManager> logger)
    {
        _options = options.Value;
        _logger = logger;
        _logBuffers[PythonEngineKey] = new ConcurrentQueue<EngineLogEntry>();
        _logBuffers[RustEngineKey] = new ConcurrentQueue<EngineLogEntry>();
    }

    /// <summary>
    /// Start all configured engine processes and begin health monitoring.
    /// </summary>
    public async Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        if (_options.PythonEngine.AutoStart)
        {
            await StartEngineAsync(PythonEngineKey, BuildPythonStartInfo(), cancellationToken);
        }

        if (_options.RustEngine.AutoStart)
        {
            await StartEngineAsync(RustEngineKey, BuildRustStartInfo(), cancellationToken);
        }

        StartWatchdog();
    }

    /// <summary>
    /// Stop all running engine processes gracefully.
    /// </summary>
    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        StopWatchdog();

        var tasks = _processes.Keys
            .Select(key => StopEngineAsync(key, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Get the current state of a specific engine process.
    /// </summary>
    public EngineProcessState GetProcessState(string engineKey)
    {
        if (!_processes.TryGetValue(engineKey, out var managed))
        {
            return new EngineProcessState
            {
                EngineKey = engineKey,
                Status = ProcessStatus.NotStarted
            };
        }

        var isRunning = !managed.Process.HasExited;
        return new EngineProcessState
        {
            EngineKey = engineKey,
            ProcessId = managed.Process.Id,
            Status = isRunning ? ProcessStatus.Running : ProcessStatus.Stopped,
            StartedAt = managed.StartedAt,
            RestartCount = managed.RestartCount,
            LastExitCode = isRunning ? null : managed.Process.ExitCode
        };
    }

    /// <summary>
    /// Get the current state of all engine processes.
    /// </summary>
    public IReadOnlyList<EngineProcessState> GetAllProcessStates()
    {
        return [GetProcessState(PythonEngineKey), GetProcessState(RustEngineKey)];
    }

    /// <summary>
    /// Get captured log entries for a specific engine.
    /// </summary>
    public IReadOnlyList<EngineLogEntry> GetLogs(string engineKey, int maxEntries = 100)
    {
        if (!_logBuffers.TryGetValue(engineKey, out var queue))
            return [];

        return queue.TakeLast(maxEntries).ToList();
    }

    /// <summary>
    /// Get captured log entries for all engines combined.
    /// </summary>
    public IReadOnlyList<EngineLogEntry> GetAllLogs(int maxEntries = 200)
    {
        return _logBuffers.Values
            .SelectMany(q => q)
            .OrderByDescending(e => e.Timestamp)
            .Take(maxEntries)
            .ToList();
    }

    private async Task StartEngineAsync(string engineKey, ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        await StopEngineAsync(engineKey, cancellationToken);

        _logger.LogInformation("Starting {Engine} engine: {FileName} {Arguments}",
            engineKey, startInfo.FileName, startInfo.Arguments);
        AddLog(engineKey, EngineLogLevel.Info, $"Starting {engineKey} engine...");

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                AddLog(engineKey, EngineLogLevel.Stdout, e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                AddLog(engineKey, EngineLogLevel.Stderr, e.Data);
            }
        };

        if (!process.Start())
        {
            AddLog(engineKey, EngineLogLevel.Error, "Failed to start process.");
            throw new InvalidOperationException($"Failed to start {engineKey} engine process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var managed = new ManagedProcess(process, DateTime.UtcNow);
        _processes[engineKey] = managed;

        _logger.LogInformation("{Engine} engine started (PID {Pid})", engineKey, process.Id);
        AddLog(engineKey, EngineLogLevel.Info, $"Process started (PID {process.Id}).");

        // Wait briefly for the process to stabilize
        await Task.Delay(500, cancellationToken);

        if (process.HasExited)
        {
            AddLog(engineKey, EngineLogLevel.Error, $"Process exited immediately with code {process.ExitCode}.");
            throw new InvalidOperationException(
                $"{engineKey} engine process exited immediately (exit code {process.ExitCode}).");
        }
    }

    private async Task StopEngineAsync(string engineKey, CancellationToken cancellationToken)
    {
        if (!_processes.TryRemove(engineKey, out var managed))
            return;

        if (managed.Process.HasExited)
        {
            managed.Process.Dispose();
            return;
        }

        _logger.LogInformation("Stopping {Engine} engine (PID {Pid})", engineKey, managed.Process.Id);
        AddLog(engineKey, EngineLogLevel.Info, $"Stopping process (PID {managed.Process.Id})...");

        try
        {
            // Attempt graceful shutdown
            managed.Process.Kill(entireProcessTree: true);
            await managed.Process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("{Engine} engine did not exit gracefully, force killing", engineKey);
            AddLog(engineKey, EngineLogLevel.Warning, "Process did not exit gracefully.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Error stopping {Engine} engine", engineKey);
        }
        finally
        {
            managed.Process.Dispose();
            AddLog(engineKey, EngineLogLevel.Info, "Process stopped.");
        }
    }

    /// <summary>
    /// Background watchdog that monitors processes and restarts crashed engines.
    /// </summary>
    private void StartWatchdog()
    {
        _watchdogCts = new CancellationTokenSource();
        _watchdogTask = RunWatchdogAsync(_watchdogCts.Token);
    }

    private void StopWatchdog()
    {
        _watchdogCts?.Cancel();
        _watchdogCts?.Dispose();
        _watchdogCts = null;
    }

    private async Task RunWatchdogAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Engine watchdog started");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                foreach (var (key, managed) in _processes)
                {
                    if (!managed.Process.HasExited)
                        continue;

                    var exitCode = managed.Process.ExitCode;
                    _logger.LogWarning("{Engine} engine crashed (exit code {Code}, restart #{Count})",
                        key, exitCode, managed.RestartCount + 1);
                    AddLog(key, EngineLogLevel.Error, $"Process crashed (exit code {exitCode}). Restarting...");

                    managed.Process.Dispose();
                    _processes.TryRemove(key, out _);

                    // Exponential backoff: 1s, 2s, 4s, 8s, capped at 30s
                    var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, managed.RestartCount), 30));
                    await Task.Delay(delay, cancellationToken);

                    try
                    {
                        var startInfo = key == PythonEngineKey ? BuildPythonStartInfo() : BuildRustStartInfo();
                        await StartEngineAsync(key, startInfo, cancellationToken);

                        if (_processes.TryGetValue(key, out var restarted))
                        {
                            _processes[key] = restarted with { RestartCount = managed.RestartCount + 1 };
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to restart {Engine} engine", key);
                        AddLog(key, EngineLogLevel.Error, $"Restart failed: {ex.Message}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        _logger.LogInformation("Engine watchdog stopped");
    }

    private ProcessStartInfo BuildPythonStartInfo()
    {
        var pythonPath = _options.PythonEngine.PythonPath ?? "python";
        var scriptPath = _options.PythonEngine.ScriptPath ?? "ai-engine/main.py";
        var port = new Uri(_options.PythonEngine.BaseUrl).Port;

        return new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"-u \"{scriptPath}\" --port {port}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
    }

    private ProcessStartInfo BuildRustStartInfo()
    {
        var binaryPath = _options.RustEngine.BinaryPath ?? "rust-engine/target/release/aimran-resource-engine";
        var port = new Uri(_options.RustEngine.BaseUrl).Port;

        return new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = $"--port {port}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
    }

    private void AddLog(string engineKey, EngineLogLevel level, string message)
    {
        if (!_logBuffers.TryGetValue(engineKey, out var queue))
            return;

        queue.Enqueue(new EngineLogEntry
        {
            EngineKey = engineKey,
            Level = level,
            Message = message
        });

        // Trim buffer if too large
        while (queue.Count > MaxLogEntries)
        {
            queue.TryDequeue(out _);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopWatchdog();

        foreach (var (_, managed) in _processes)
        {
            try
            {
                if (!managed.Process.HasExited)
                {
                    managed.Process.Kill(entireProcessTree: true);
                }

                managed.Process.Dispose();
            }
            catch
            {
                // Best-effort cleanup
            }
        }

        _processes.Clear();
    }

    /// <summary>
    /// Internal state for a managed process.
    /// </summary>
    private sealed record ManagedProcess(Process Process, DateTime StartedAt)
    {
        public int RestartCount { get; init; }
    }
}

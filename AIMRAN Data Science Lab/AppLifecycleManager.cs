using System;
using System.Threading.Tasks;
using AimranDataScienceLab.Engine;
using Microsoft.Maui.Storage;

namespace AIMRAN_Data_Science_Lab;

/// <summary>
/// Application lifecycle management for MAUI desktop deployment.
/// Handles engine startup, shutdown, and resource cleanup.
/// </summary>
public static class AppLifecycleManager
{
    private static IEngineManager? _engineManager;
    private static bool _isInitialized = false;

    /// <summary>
    /// Initialize app on startup (call from App.xaml.cs in OnStart)
    /// </summary>
    public static async Task InitializeAsync(IEngineManager engineManager)
    {
        if (_isInitialized)
            return;

        try
        {
            _engineManager = engineManager;

            // Initialize engine resources (creates folders, detects engines)
            await EngineResourceInitializer.InitializeEnginesAsync();

            // Auto-start configured engines
            await AutoStartEnginesAsync();

            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine("✅ App lifecycle initialization complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ App lifecycle initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Shutdown app gracefully (call from App.xaml.cs in OnDestroy)
    /// </summary>
    public static async Task ShutdownAsync()
    {
        try
        {
            if (_engineManager != null)
            {
                System.Diagnostics.Debug.WriteLine("Shutting down engines...");
                await _engineManager.ShutdownAllAsync();
                (_engineManager as IDisposable)?.Dispose();
            }

            System.Diagnostics.Debug.WriteLine("✅ App shutdown complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ App shutdown error: {ex.Message}");
        }
    }

    /// <summary>
    /// Auto-start engines based on user settings
    /// </summary>
    private static async Task AutoStartEnginesAsync()
    {
        if (_engineManager == null)
            return;

        try
        {
            // Check user preferences (would typically come from app settings)
            var autoStartEnabled = Preferences.Default.Get("AutoStartEngines", true);

            if (autoStartEnabled)
            {
                // Try to auto-detect and start available engines
                var pythonAvailable = await _engineManager.IsEngineAvailableAsync(ExternalEngine.Python);
                if (pythonAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("Auto-starting Python engine...");
                    await _engineManager.StartEngineAsync(ExternalEngine.Python);
                }

                var rustAvailable = await _engineManager.IsEngineAvailableAsync(ExternalEngine.Rust);
                if (rustAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("Auto-starting Rust engine...");
                    await _engineManager.StartEngineAsync(ExternalEngine.Rust);
                }

                var rAvailable = await _engineManager.IsEngineAvailableAsync(ExternalEngine.R);
                if (rAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("Auto-starting R engine...");
                    await _engineManager.StartEngineAsync(ExternalEngine.R);
                }
            }

            // Monitor engine status and log
            _ = MonitorEnginesInBackgroundAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-start engines error: {ex.Message}");
        }
    }

    /// <summary>
    /// Background task to monitor engine health
    /// </summary>
    private static async Task MonitorEnginesInBackgroundAsync()
    {
        if (_engineManager == null)
            return;

        try
        {
            var cts = new System.Threading.CancellationTokenSource();

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var status = await _engineManager.GetStatusAsync();

                    // Log status for debugging
                    System.Diagnostics.Debug.WriteLine($"[Engine Status] Python: {status.Python.Status}, Rust: {status.Rust.Status}, R: {status.R.Status}");

                    // Check for crashed engines and restart if auto-restart enabled
                    var autoRestartEnabled = Preferences.Default.Get("AutoRestartEngines", false);
                    if (autoRestartEnabled)
                    {
                        if (status.Python.Status == EngineStatus.Error)
                        {
                            System.Diagnostics.Debug.WriteLine("Python engine error detected, restarting...");
                            await _engineManager.RestartEngineAsync(ExternalEngine.Python);
                        }

                        if (status.Rust.Status == EngineStatus.Error)
                        {
                            System.Diagnostics.Debug.WriteLine("Rust engine error detected, restarting...");
                            await _engineManager.RestartEngineAsync(ExternalEngine.Rust);
                        }
                    }

                    await Task.Delay(30000, cts.Token); // Check every 30 seconds
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Engine monitoring error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Monitor engines background task error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current engine status snapshot
    /// </summary>
    public static async Task<EngineStatusSnapshot?> GetEngineStatusAsync()
    {
        try
        {
            return _engineManager != null ? await _engineManager.GetStatusAsync() : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Manually start an engine
    /// </summary>
    public static async Task<bool> StartEngineAsync(ExternalEngine engine)
    {
        try
        {
            return _engineManager != null && await _engineManager.StartEngineAsync(engine);
        }
        catch { return false; }
    }

    /// <summary>
    /// Manually stop an engine
    /// </summary>
    public static async Task<bool> StopEngineAsync(ExternalEngine engine)
    {
        try
        {
            return _engineManager != null && await _engineManager.StopEngineAsync(engine);
        }
        catch { return false; }
    }

    /// <summary>
    /// Get engine logs
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetEngineLogsAsync(ExternalEngine engine, int lineCount = 50)
    {
        try
        {
            return _engineManager != null
                ? await _engineManager.GetEngineLogsAsync(engine, lineCount)
                : new List<string>().AsReadOnly();
        }
        catch { return new List<string>().AsReadOnly(); }
    }
}

using System.Collections.Concurrent;
using System.Runtime.Loader;
using System.Text.Json;
using AIMRAN_Data_Science_Lab.Extensions;
using AIMRAN_Data_Science_Lab.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Discovers, loads, and manages extensions from the local extensions directory.
/// Each extension is loaded in its own <see cref="AssemblyLoadContext"/> for isolation.
/// </summary>
internal sealed class LocalExtensionManagerService : IExtensionManagerService, IDisposable
{
    private readonly string _extensionsRoot;
    private readonly IPythonExecutionService _pythonExecution;
    private readonly IRExecutionService _rExecution;
    private readonly ILogger<LocalExtensionManagerService> _logger;

    private readonly ConcurrentDictionary<string, LoadedExtension> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ExtensionInfo> _registry = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public LocalExtensionManagerService(
        IPythonExecutionService pythonExecution,
        IRExecutionService rExecution,
        ILogger<LocalExtensionManagerService> logger)
    {
        _pythonExecution = pythonExecution;
        _rExecution = rExecution;
        _logger = logger;

        // Walk up from the base directory to find or create 'extensions' folder
        _extensionsRoot = ResolveExtensionsDirectory();
    }

    public event EventHandler<ExtensionStateChangedEventArgs>? ExtensionStateChanged;

    #region Discovery

    public async Task<IReadOnlyList<ExtensionInfo>> DiscoverExtensionsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_extensionsRoot))
        {
            Directory.CreateDirectory(_extensionsRoot);
            _logger.LogInformation("Created extensions directory: {Path}", _extensionsRoot);
        }

        var directories = Directory.GetDirectories(_extensionsRoot);
        foreach (var dir in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("No manifest.json in {Dir}, skipping", dir);
                continue;
            }

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<ExtensionManifest>(json, ManifestJsonOptions);
                if (manifest is null)
                {
                    _logger.LogWarning("Invalid manifest in {Dir}", dir);
                    continue;
                }

                var info = new ExtensionInfo
                {
                    Manifest = manifest,
                    DirectoryPath = dir,
                    State = ExtensionState.Discovered
                };

                _registry[manifest.Id] = info;
                _logger.LogInformation("Discovered extension: {Name} v{Version}", manifest.Name, manifest.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read manifest from {Dir}", dir);
            }
        }

        return _registry.Values.ToList();
    }

    public IReadOnlyList<ExtensionInfo> GetAllExtensions() => _registry.Values.ToList();

    public ExtensionInfo? GetExtension(string extensionId) =>
        _registry.GetValueOrDefault(extensionId);

    #endregion

    #region Enable / Disable

    public async Task<ExtensionInfo> EnableExtensionAsync(string extensionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);

        if (!_registry.TryGetValue(extensionId, out var info))
            throw new InvalidOperationException($"Extension '{extensionId}' not found. Run DiscoverExtensionsAsync first.");

        if (info.State == ExtensionState.Active)
            return info;

        UpdateState(extensionId, ExtensionState.Loading);

        try
        {
            var assemblyPath = Path.Combine(info.DirectoryPath, info.Manifest.EntryAssembly);
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException($"Entry assembly not found: {assemblyPath}");

            // Load in an isolated context
            var loadContext = new AssemblyLoadContext($"ext-{extensionId}", isCollectible: true);
            var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));

            var extensionType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IAimranExtension).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            if (extensionType is null)
                throw new InvalidOperationException($"No IAimranExtension implementation found in {info.Manifest.EntryAssembly}");

            var extension = (IAimranExtension)Activator.CreateInstance(extensionType)!;

            var context = new ExtensionContext(
                info.DirectoryPath,
                _pythonExecution,
                _rExecution,
                _logger);

            await extension.InitializeAsync(context, cancellationToken);
            await extension.ActivateAsync(cancellationToken);

            var loaded = new LoadedExtension(extension, context, loadContext);
            _loaded[extensionId] = loaded;

            var updated = info with { State = ExtensionState.Active, ActivatedAt = DateTime.UtcNow, ErrorMessage = null };
            _registry[extensionId] = updated;
            RaiseStateChanged(extensionId, ExtensionState.Active);

            _logger.LogInformation("Extension activated: {Name}", info.Manifest.Name);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable extension {Id}", extensionId);
            var errored = info with { State = ExtensionState.Error, ErrorMessage = ex.Message };
            _registry[extensionId] = errored;
            RaiseStateChanged(extensionId, ExtensionState.Error);
            return errored;
        }
    }

    public async Task<ExtensionInfo> DisableExtensionAsync(string extensionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);

        if (!_registry.TryGetValue(extensionId, out var info))
            throw new InvalidOperationException($"Extension '{extensionId}' not found.");

        if (_loaded.TryRemove(extensionId, out var loaded))
        {
            try
            {
                await loaded.Extension.DeactivateAsync(cancellationToken);
                await loaded.Extension.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during extension deactivation: {Id}", extensionId);
            }

            loaded.LoadContext.Unload();
        }

        var updated = info with { State = ExtensionState.Disabled, ActivatedAt = null };
        _registry[extensionId] = updated;
        RaiseStateChanged(extensionId, ExtensionState.Disabled);

        _logger.LogInformation("Extension disabled: {Name}", info.Manifest.Name);
        return updated;
    }

    #endregion

    #region Install / Uninstall

    public async Task<ExtensionInfo> InstallExtensionAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"Source path not found: {sourcePath}");

        var manifestPath = Path.Combine(sourcePath, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("manifest.json not found in source directory.");

        var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var manifest = JsonSerializer.Deserialize<ExtensionManifest>(json, ManifestJsonOptions)
            ?? throw new InvalidOperationException("Invalid manifest.json");

        var destDir = Path.Combine(_extensionsRoot, manifest.Id);
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, recursive: true);

        CopyDirectory(sourcePath, destDir);

        var info = new ExtensionInfo
        {
            Manifest = manifest,
            DirectoryPath = destDir,
            State = ExtensionState.Discovered
        };

        _registry[manifest.Id] = info;
        _logger.LogInformation("Extension installed: {Name} v{Version}", manifest.Name, manifest.Version);
        return info;
    }

    public async Task UninstallExtensionAsync(string extensionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionId);

        if (_loaded.ContainsKey(extensionId))
        {
            await DisableExtensionAsync(extensionId, cancellationToken);
        }

        if (_registry.TryRemove(extensionId, out var info))
        {
            if (Directory.Exists(info.DirectoryPath))
            {
                Directory.Delete(info.DirectoryPath, recursive: true);
            }

            _logger.LogInformation("Extension uninstalled: {Name}", info.Manifest.Name);
        }
    }

    #endregion

    #region Commands

    public IReadOnlyList<ExtensionCommand> GetRegisteredCommands()
    {
        return _loaded.Values
            .SelectMany(l => l.Context.RegisteredCommands)
            .ToList();
    }

    public async Task ExecuteCommandAsync(string commandId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        var command = GetRegisteredCommands().FirstOrDefault(c => c.Id == commandId)
            ?? throw new InvalidOperationException($"Command '{commandId}' not found.");

        await command.ExecuteAsync(cancellationToken);
    }

    #endregion

    #region Helpers

    private void UpdateState(string extensionId, ExtensionState state)
    {
        if (_registry.TryGetValue(extensionId, out var info))
        {
            _registry[extensionId] = info with { State = state };
            RaiseStateChanged(extensionId, state);
        }
    }

    private void RaiseStateChanged(string extensionId, ExtensionState state) =>
        ExtensionStateChanged?.Invoke(this, new ExtensionStateChangedEventArgs(extensionId, state));

    private static string ResolveExtensionsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "extensions");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        // Fallback: create next to the app
        var fallback = Path.Combine(AppContext.BaseDirectory, "extensions");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(source))
        {
            CopyDirectory(subDir, Path.Combine(destination, Path.GetFileName(subDir)));
        }
    }

    public void Dispose()
    {
        foreach (var loaded in _loaded.Values)
        {
            try
            {
                loaded.Extension.DeactivateAsync(CancellationToken.None).GetAwaiter().GetResult();
                loaded.Extension.DisposeAsync().AsTask().GetAwaiter().GetResult();
                loaded.LoadContext.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing extension: {Id}", loaded.Extension.Id);
            }
        }

        _loaded.Clear();
    }

    #endregion

    /// <summary>
    /// Tracks a loaded extension with its context and load context for unloading.
    /// </summary>
    private sealed record LoadedExtension(
        IAimranExtension Extension,
        ExtensionContext Context,
        AssemblyLoadContext LoadContext);
}

using System.Diagnostics;
using System.Text.RegularExpressions;
using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local implementation that shells out to pip/cargo for package management
/// and delegates model versioning to <see cref="IModelService"/>.
/// </summary>
public sealed partial class LocalPluginManagerService : IPluginManagerService
{
    private readonly IModelService _modelService;

    public LocalPluginManagerService(IModelService modelService)
    {
        _modelService = modelService;
    }

    #region Python Packages

    public async Task<IReadOnlyList<PythonPackage>> ListPythonPackagesAsync(CancellationToken cancellationToken = default)
    {
        var output = await RunProcessAsync("pip", "list --format=json", cancellationToken);
        if (output is null)
            return [];

        var packages = new List<PythonPackage>();
        // pip list --format=json returns [{"name":"...", "version":"..."},...]
        foreach (var match in PipJsonEntryRegex().EnumerateMatches(output))
        {
            var segment = output.AsSpan(match.Index, match.Length);
            var nameMatch = PipNameRegex().Match(segment.ToString());
            var versionMatch = PipVersionRegex().Match(segment.ToString());

            if (nameMatch.Success && versionMatch.Success)
            {
                packages.Add(new PythonPackage
                {
                    Name = nameMatch.Groups[1].Value,
                    InstalledVersion = versionMatch.Groups[1].Value
                });
            }
        }

        return packages;
    }

    public async Task<PackageOperationResult> InstallPythonPackageAsync(
        PackageInstallRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sw = Stopwatch.StartNew();
        var args = request.Upgrade
            ? $"install --upgrade {request.PackageName}"
            : string.IsNullOrEmpty(request.Version)
                ? $"install {request.PackageName}"
                : $"install {request.PackageName}=={request.Version}";

        var (exitCode, stdout, stderr) = await RunProcessDetailedAsync("pip", args, cancellationToken);
        sw.Stop();

        return new PackageOperationResult
        {
            PackageName = request.PackageName,
            Status = exitCode == 0 ? PackageOperationStatus.Success : PackageOperationStatus.Failed,
            Output = stdout,
            ErrorMessage = exitCode != 0 ? stderr : null,
            Duration = sw.Elapsed
        };
    }

    public async Task<PackageOperationResult> UninstallPythonPackageAsync(
        string packageName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var sw = Stopwatch.StartNew();
        var (exitCode, stdout, stderr) = await RunProcessDetailedAsync("pip", $"uninstall -y {packageName}", cancellationToken);
        sw.Stop();

        return new PackageOperationResult
        {
            PackageName = packageName,
            Status = exitCode == 0 ? PackageOperationStatus.Success : PackageOperationStatus.Failed,
            Output = stdout,
            ErrorMessage = exitCode != 0 ? stderr : null,
            Duration = sw.Elapsed
        };
    }

    public async Task<IReadOnlyList<PythonPackage>> CheckPythonUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var output = await RunProcessAsync("pip", "list --outdated --format=json", cancellationToken);
        if (output is null)
            return [];

        var packages = new List<PythonPackage>();
        foreach (var match in PipOutdatedEntryRegex().EnumerateMatches(output))
        {
            var segment = output.Substring(match.Index, match.Length);
            var nameMatch = PipNameRegex().Match(segment);
            var versionMatch = PipVersionRegex().Match(segment);
            var latestMatch = PipLatestVersionRegex().Match(segment);

            if (nameMatch.Success && versionMatch.Success)
            {
                packages.Add(new PythonPackage
                {
                    Name = nameMatch.Groups[1].Value,
                    InstalledVersion = versionMatch.Groups[1].Value,
                    LatestVersion = latestMatch.Success ? latestMatch.Groups[1].Value : null
                });
            }
        }

        return packages;
    }

    #endregion

    #region Rust Crates

    public async Task<IReadOnlyList<RustCrate>> ListRustCratesAsync(CancellationToken cancellationToken = default)
    {
        // Parse Cargo.toml [dependencies] section
        var cargoPath = FindCargoToml();
        if (cargoPath is null)
            return [];

        var content = await File.ReadAllTextAsync(cargoPath, cancellationToken);
        var crates = new List<RustCrate>();

        var inDeps = false;
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("[dependencies]", StringComparison.OrdinalIgnoreCase))
            {
                inDeps = true;
                continue;
            }

            if (trimmed.StartsWith('[') && inDeps)
                break;

            if (!inDeps || string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            var crateMatch = CargoDepRegex().Match(trimmed);
            if (crateMatch.Success)
            {
                var name = crateMatch.Groups[1].Value;
                var version = crateMatch.Groups[2].Value;
                crates.Add(new RustCrate
                {
                    Name = name,
                    InstalledVersion = version
                });
            }
        }

        return crates;
    }

    public async Task<PackageOperationResult> AddRustCrateAsync(
        PackageInstallRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sw = Stopwatch.StartNew();
        var cargoDir = FindCargoDir();
        var args = string.IsNullOrEmpty(request.Version)
            ? $"add {request.PackageName}"
            : $"add {request.PackageName}@{request.Version}";

        var (exitCode, stdout, stderr) = await RunProcessDetailedAsync(
            "cargo", args, cancellationToken, cargoDir);
        sw.Stop();

        return new PackageOperationResult
        {
            PackageName = request.PackageName,
            Status = exitCode == 0 ? PackageOperationStatus.Success : PackageOperationStatus.Failed,
            Output = stdout,
            ErrorMessage = exitCode != 0 ? stderr : null,
            Duration = sw.Elapsed
        };
    }

    public async Task<PackageOperationResult> RemoveRustCrateAsync(
        string crateName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(crateName);

        var sw = Stopwatch.StartNew();
        var cargoDir = FindCargoDir();
        var (exitCode, stdout, stderr) = await RunProcessDetailedAsync(
            "cargo", $"remove {crateName}", cancellationToken, cargoDir);
        sw.Stop();

        return new PackageOperationResult
        {
            PackageName = crateName,
            Status = exitCode == 0 ? PackageOperationStatus.Success : PackageOperationStatus.Failed,
            Output = stdout,
            ErrorMessage = exitCode != 0 ? stderr : null,
            Duration = sw.Elapsed
        };
    }

    #endregion

    #region Model Versioning

    public async Task<IReadOnlyList<ModelVersionEntry>> ListModelVersionsAsync(CancellationToken cancellationToken = default)
    {
        var models = await _modelService.GetAllAsync(cancellationToken);
        return models
            .Select(m => new ModelVersionEntry
            {
                ModelId = m.Id,
                Name = m.Name,
                Version = m.Version,
                Algorithm = m.Algorithm,
                Framework = m.Framework,
                SizeBytes = m.SizeBytes,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                Metrics = m.PerformanceMetrics
            })
            .OrderBy(m => m.Name)
            .ThenByDescending(m => m.Version)
            .ToList();
    }

    public async Task<ModelVersionEntry> CreateModelVersionAsync(
        Guid modelId, string newFilePath, CancellationToken cancellationToken = default)
    {
        var model = await _modelService.CreateVersionAsync(modelId, newFilePath, cancellationToken);
        return new ModelVersionEntry
        {
            ModelId = model.Id,
            Name = model.Name,
            Version = model.Version,
            Algorithm = model.Algorithm,
            Framework = model.Framework,
            SizeBytes = model.SizeBytes,
            Status = model.Status,
            CreatedAt = model.CreatedAt,
            Metrics = model.PerformanceMetrics
        };
    }

    public async Task DeleteModelVersionAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        await _modelService.DeleteAsync(modelId, cancellationToken);
    }

    #endregion

    #region Process Helpers

    private static async Task<string?> RunProcessAsync(
        string fileName, string arguments, CancellationToken cancellationToken)
    {
        var (exitCode, stdout, _) = await RunProcessDetailedAsync(fileName, arguments, cancellationToken);
        return exitCode == 0 ? stdout : null;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessDetailedAsync(
        string fileName, string arguments, CancellationToken cancellationToken, string? workingDirectory = null)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? ""
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string? FindCargoToml()
    {
        var dir = FindCargoDir();
        if (dir is null) return null;
        var path = Path.Combine(dir, "Cargo.toml");
        return File.Exists(path) ? path : null;
    }

    private static string? FindCargoDir()
    {
        // Walk up from AppContext.BaseDirectory to find rust-engine/Cargo.toml
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "rust-engine");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "Cargo.toml")))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    #endregion

    #region Regex

    [GeneratedRegex("""\{[^}]+\}""")]
    private static partial Regex PipJsonEntryRegex();

    [GeneratedRegex("""\{[^}]+\}""")]
    private static partial Regex PipOutdatedEntryRegex();

    [GeneratedRegex(""""name"\s*:\s*"([^"]+)"""")]
    private static partial Regex PipNameRegex();

    [GeneratedRegex(""""version"\s*:\s*"([^"]+)"""")]
    private static partial Regex PipVersionRegex();

    [GeneratedRegex(""""latest_version"\s*:\s*"([^"]+)"""")]
    private static partial Regex PipLatestVersionRegex();

    [GeneratedRegex("""^(\S+)\s*=\s*(?:\{[^}]*version\s*=\s*"([^"]+)"|\s*"([^"]+)")""")]
    private static partial Regex CargoDepRegex();

    #endregion
}

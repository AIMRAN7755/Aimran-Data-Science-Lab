using AIMRAN_Data_Science_Lab.Services;
using AimranDataScienceLab.Gateway;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Manages Python packages via the Gateway's Python AI Engine.
/// </summary>
internal sealed class GatewayPackageManagerService : IPackageManagerService
{
    private readonly IGatewayManager _gateway;

    public GatewayPackageManagerService(IGatewayManager gateway)
    {
        _gateway = gateway;
    }

    public async Task<IReadOnlyList<PackageInfo>> ListPackagesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _gateway.PythonEngine.ListPackagesAsync(cancellationToken);
        return result.Packages
            .Select(p => new PackageInfo
            {
                Name = p.Name,
                Version = p.Version,
                Summary = p.Summary,
                LatestVersion = p.LatestVersion
            })
            .ToList();
    }

    public async Task<PackageActionResult> InstallAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _gateway.PythonEngine.InstallPackageAsync(packageName, version, cancellationToken);
        return new PackageActionResult
        {
            Success = result.Success,
            PackageName = result.PackageName,
            Message = result.Message
        };
    }

    public async Task<PackageActionResult> UninstallAsync(
        string packageName,
        CancellationToken cancellationToken = default)
    {
        var result = await _gateway.PythonEngine.UninstallPackageAsync(packageName, cancellationToken);
        return new PackageActionResult
        {
            Success = result.Success,
            PackageName = result.PackageName,
            Message = result.Message
        };
    }
}

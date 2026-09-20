using Microsoft.Extensions.DependencyInjection;
using AimranDataScienceLab.Core.DataManagement;
using AimranDataScienceLab.Core.ML;
using AimranDataScienceLab.Core.Monitoring;
using AimranDataScienceLab.Core.Security;

namespace AimranDataScienceLab.Core;

/// <summary>
/// Dependency injection extensions for registering all Core services.
/// Includes data management, ML infrastructure, monitoring, and security services.
/// </summary>
public static class CoreServiceExtensions
{
    /// <summary>
    /// Register all AIMRAN Core services in the DI container.
    /// Should be called during application startup in the service configuration phase.
    /// </summary>
    public static IServiceCollection AddAimranCoreServices(
        this IServiceCollection services)
    {
        // ────────── Data Management Services ──────────────────────────────────
        services.AddSingleton<IDataLakeService, DataLakeService>();
        services.AddSingleton<IDataWarehouseService, DataWarehouseService>();
        services.AddSingleton<IMasterDataManagementService, MasterDataManagementService>();
        services.AddSingleton<IDataCatalogService, DataCatalogService>();

        // ────────── Machine Learning Services ──────────────────────────────────
        services.AddSingleton<IFeatureStoreService, FeatureStoreService>();
        services.AddSingleton<IModelRegistryService, ModelRegistryService>();
        services.AddSingleton<IExperimentTrackingService, ExperimentTrackingService>();

        // ────────── Monitoring & Observability Services ──────────────────────────────────
        services.AddSingleton<IMetricsCollectorService, MetricsCollectorService>();
        services.AddSingleton<IObservabilityService, ObservabilityService>();

        // ────────── Security & Authentication Services ──────────────────────────────────
        services.AddSingleton<IAuthenticationProviderService, AuthenticationProviderService>();
        services.AddSingleton<ISecretsManagerService, SecretsManagerService>();

        return services;
    }
}

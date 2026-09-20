using AIMRAN_Data_Science_Lab.Services;
using AIMRAN_Data_Science_Lab.Services.Auth;
using AIMRAN_Data_Science_Lab.Services.Azure;
using AIMRAN_Data_Science_Lab.Services.Local;
using AIMRAN_Data_Science_Lab.Services.Sqlite;
using AIMRAN_Data_Science_Lab.Services.Collaboration;
using AIMRAN_Data_Science_Lab.Services.Notifications;
using AimranDataScienceLab.Engine.Data;
using AimranDataScienceLab.Gateway;
using AimranDataScienceLab.Gateway.Clients;
using AimranDataScienceLab.Core;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace AimranDataScienceLab.Engine;

/// <summary>
/// One-call registration of every AIMRAN Data Science Lab service.
/// Used by both the MAUI Blazor Hybrid host and the Blazor Server Web host
/// so the service graph is identical in both deployment targets.
/// </summary>
public static class DataScienceLabServiceExtensions
{
    /// <summary>
    /// Register the full AIMRAN Data Science Lab service stack:
    /// Engine core, SQLite persistence, data cleaning, dataset versioning,
    /// Azure cloud services, the Python/Rust service gateway, and the plugin manager.
    /// </summary>
    public static IServiceCollection AddAimranDataScienceLab(
        this IServiceCollection services,
        Action<EngineOptions>? configureEngine = null,
        Action<Gateway.Configuration.GatewayOptions>? configureGateway = null)
    {
        // ── Engine Core ──────────────────────────────────────────────────
        services.AddAimranEngine(configureEngine);

        // ── SQLite Persistence Layer ─────────────────────────────────────
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<SqliteConnectionFactory>();
            DatabaseInitializer.Initialize(factory);
            return factory;
        });

        // Core services — SQLite-backed (persistent)
        services.AddSingleton<IProjectService, SqliteProjectService>();
        services.AddSingleton<IDatasetService, SqliteDatasetService>();
        services.AddSingleton<IExperimentService, SqliteExperimentService>();
        services.AddSingleton<IModelService, SqliteModelService>();

        // ── Resource Monitoring (real-time system metrics) ───────────────
        services.AddSingleton<IResourceMonitorService, LocalResourceMonitorService>();

        // ── Data Cleaning Services ───────────────────────────────────────
        services.AddSingleton<IDataProfilingService, LocalDataProfilingService>();
        services.AddSingleton<IOutlierDetectionService, LocalOutlierDetectionService>();
        services.AddSingleton<IDataCleaningService, LocalDataCleaningService>();
        services.AddSingleton<ICleaningRuleService, LocalCleaningRuleService>();

        // ── Dataset Versioning ───────────────────────────────────────────
        services.AddSingleton<IDatasetStorageProvider, LocalDatasetStorageProvider>();
        services.AddSingleton<IDeltaEngine, LocalDeltaEngine>();
        services.AddSingleton<IDatasetVersionService, LocalDatasetVersionService>();

        // ── Authentication & Authorization ────────────────────────────
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IAuthService, LocalAuthService>();
        services.AddSingleton<AuthenticationStateProvider, AimranAuthStateProvider>();
        services.AddSingleton<IEngineTokenProvider, AuthenticatedTokenProvider>();
        services.AddAuthorizationCore();

        // ── Authorization, Audit, Security ───────────────────────────────
        services.AddSingleton<IAuthorizationService, LocalAuthorizationService>();
        services.AddSingleton<IAuditService, LocalAuditService>();
        services.AddSingleton<IExecutionSandbox, SecureExecutionService>();
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        services.AddSingleton<ISecretStore, LocalSecretStore>();
        services.AddSingleton<ISessionManager, SessionManager>();

        // ── Azure Cloud Services ─────────────────────────────────────
        services.AddSingleton<IAzureConfigService, AzureConfigService>();
        services.AddSingleton<IAzureStorageService, AzureStorageService>();
        services.AddSingleton<IAzureMlService, AzureMlService>();

        // ── Service Gateway Layer (Python AI Engine + Rust Resource Engine)
        services.AddAimranGateway(configureGateway);

        // ── Gateway-Backed Services (code execution, packages, visualization) ─
        services.AddSingleton<IPythonExecutionService, GatewayPythonExecutionService>();
        services.AddSingleton<IRExecutionService, GatewayRExecutionService>();
        services.AddSingleton<IPackageManagerService, GatewayPackageManagerService>();
        services.AddSingleton<IVisualizationService, GatewayVisualizationService>();

        // ── Plugin Manager ───────────────────────────────────────────────
        services.AddSingleton<IPluginManagerService, LocalPluginManagerService>();

        // ── Extension System ─────────────────────────────────────────────
        services.AddSingleton<IExtensionManagerService, LocalExtensionManagerService>();

        // ── Administration ───────────────────────────────────────────────
        services.AddSingleton<IAdminService, LocalAdminService>();

        // ── Collaboration & Notifications ─────────────────────────────────
        services.AddSingleton<IPresenceService, PresenceService>();
        services.AddSingleton<ICollaborationService, CollaborationService>();
        services.AddSingleton<IConflictResolutionService, ConflictResolutionService>();
        services.AddSingleton<IRealTimeNotificationService, RealTimeNotificationService>();

        return services;
    }
}

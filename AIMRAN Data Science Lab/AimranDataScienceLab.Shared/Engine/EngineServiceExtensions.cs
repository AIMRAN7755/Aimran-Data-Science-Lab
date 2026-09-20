using Microsoft.Extensions.DependencyInjection;

namespace AimranDataScienceLab.Engine;

/// <summary>
/// Extension methods for registering the AIMRAN Engine in the DI container.
/// </summary>
public static class EngineServiceExtensions
{
    /// <summary>
    /// Register <see cref="IAimranEngine"/> and its options in the DI container.
    /// </summary>
    public static IServiceCollection AddAimranEngine(
        this IServiceCollection services,
        Action<EngineOptions>? configure = null)
    {
        services.AddOptions<EngineOptions>();

        if (configure is not null)
            services.Configure(configure);

        services.AddSingleton<IAimranEngine, AimranEngine>();

        return services;
    }
}

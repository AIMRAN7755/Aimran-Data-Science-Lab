using AimranDataScienceLab.Gateway.Clients;
using AimranDataScienceLab.Gateway.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AimranDataScienceLab.Gateway;

/// <summary>
/// Extension methods for registering Gateway services in the DI container.
/// </summary>
public static class GatewayServiceExtensions
{
    /// <summary>
    /// Register the Service Gateway layer including Python and Rust engine clients,
    /// process management, retry resilience, and authentication handlers.
    /// </summary>
    public static IServiceCollection AddAimranGateway(
        this IServiceCollection services,
        Action<GatewayOptions>? configure = null)
    {
        services.AddOptions<GatewayOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        // ── Auth token provider (registered by DataScienceLabServiceExtensions) ──
        // If no provider has been registered yet, fall back to NoOp.
        services.TryAddSingleton<IEngineTokenProvider, NoOpTokenProvider>();

        // ── Delegating handlers (transient per HTTP pipeline convention) ──
        services.AddTransient<RetryDelegatingHandler>();
        services.AddTransient<AuthTokenDelegatingHandler>();

        // ── Python engine HTTP client with resilience pipeline ──
        services.AddHttpClient<IPythonEngineClient, PythonEngineClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayOptions>>().Value;
            client.BaseAddress = new Uri(options.PythonEngine.BaseUrl);
            client.Timeout = options.PythonEngine.Timeout;
        })
        .AddHttpMessageHandler<AuthTokenDelegatingHandler>()
        .AddHttpMessageHandler<RetryDelegatingHandler>();

        // ── Rust engine HTTP client with resilience pipeline ──
        services.AddHttpClient<IRustEngineClient, RustEngineClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayOptions>>().Value;
            client.BaseAddress = new Uri(options.RustEngine.BaseUrl);
            client.Timeout = options.RustEngine.Timeout;
        })
        .AddHttpMessageHandler<AuthTokenDelegatingHandler>()
        .AddHttpMessageHandler<RetryDelegatingHandler>();

        // ── R engine HTTP client with resilience pipeline ──
        services.AddHttpClient<IREngineClient, REngineClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GatewayOptions>>().Value;
            client.BaseAddress = new Uri(options.REngine.BaseUrl);
            client.Timeout = options.REngine.Timeout;
        })
        .AddHttpMessageHandler<AuthTokenDelegatingHandler>()
        .AddHttpMessageHandler<RetryDelegatingHandler>();

        // ── Process management ──
        services.AddSingleton<EngineProcessManager>();

        // ── Gateway coordinator ──
        services.AddSingleton<IGatewayManager, GatewayManager>();

        return services;
    }
}

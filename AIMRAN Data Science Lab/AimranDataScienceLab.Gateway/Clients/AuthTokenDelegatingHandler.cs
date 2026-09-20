using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Gateway.Clients;

/// <summary>
/// HTTP delegating handler that injects a bearer authentication token
/// into outgoing requests to external engine APIs.
/// </summary>
internal sealed class AuthTokenDelegatingHandler : DelegatingHandler
{
    private readonly IEngineTokenProvider _tokenProvider;
    private readonly ILogger<AuthTokenDelegatingHandler> _logger;

    public AuthTokenDelegatingHandler(
        IEngineTokenProvider tokenProvider,
        ILogger<AuthTokenDelegatingHandler> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Provides authentication tokens for engine API requests.
/// </summary>
public interface IEngineTokenProvider
{
    /// <summary>
    /// Get the current authentication token, or null if no auth is configured.
    /// </summary>
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default token provider that returns no token (open API, no auth required).
/// Replace with a real implementation when engine auth is enabled.
/// </summary>
public sealed class NoOpTokenProvider : IEngineTokenProvider
{
    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}

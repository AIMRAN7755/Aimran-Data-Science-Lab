using System.Net;
using AimranDataScienceLab.Gateway.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AimranDataScienceLab.Gateway.Clients;

/// <summary>
/// HTTP delegating handler that retries failed requests with exponential backoff.
/// Handles transient HTTP errors (5xx, 408, 429) and network failures.
/// </summary>
internal sealed class RetryDelegatingHandler : DelegatingHandler
{
    private readonly GatewayOptions _options;
    private readonly ILogger<RetryDelegatingHandler> _logger;

    private static readonly HashSet<HttpStatusCode> s_retryableStatusCodes =
    [
        HttpStatusCode.RequestTimeout,          // 408
        HttpStatusCode.TooManyRequests,          // 429
        HttpStatusCode.InternalServerError,      // 500
        HttpStatusCode.BadGateway,               // 502
        HttpStatusCode.ServiceUnavailable,       // 503
        HttpStatusCode.GatewayTimeout            // 504
    ];

    public RetryDelegatingHandler(
        IOptions<GatewayOptions> options,
        ILogger<RetryDelegatingHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var maxRetries = _options.MaxRetryAttempts;
        var baseDelay = _options.RetryDelay;
        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Clone the request for retries (the original stream may have been consumed)
                using var clonedRequest = attempt == 0 ? request : await CloneRequestAsync(request);

                response = await base.SendAsync(
                    attempt == 0 ? request : clonedRequest, cancellationToken);

                if (!ShouldRetry(response.StatusCode) || attempt == maxRetries)
                {
                    return response;
                }

                // Check for Retry-After header
                var retryAfter = GetRetryAfterDelay(response);
                var delay = retryAfter ?? TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds * Math.Pow(2, attempt));

                _logger.LogWarning(
                    "Request to {Uri} returned {StatusCode}, retrying in {Delay}ms (attempt {Attempt}/{Max})",
                    request.RequestUri, (int)response.StatusCode, delay.TotalMilliseconds, attempt + 1, maxRetries);

                response.Dispose();
                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds * Math.Pow(2, attempt));

                _logger.LogWarning(ex,
                    "Request to {Uri} failed with network error, retrying in {Delay}ms (attempt {Attempt}/{Max})",
                    request.RequestUri, delay.TotalMilliseconds, attempt + 1, maxRetries);

                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex) when (attempt < maxRetries)
            {
                // Timeout (not user-initiated cancellation)
                var delay = TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds * Math.Pow(2, attempt));

                _logger.LogWarning(ex,
                    "Request to {Uri} timed out, retrying in {Delay}ms (attempt {Attempt}/{Max})",
                    request.RequestUri, delay.TotalMilliseconds, attempt + 1, maxRetries);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // Should not reach here, but safety net
        return response ?? throw new HttpRequestException("All retry attempts exhausted.");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        s_retryableStatusCodes.Contains(statusCode);

    private static TimeSpan? GetRetryAfterDelay(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter is null)
            return null;

        if (response.Headers.RetryAfter.Delta.HasValue)
            return response.Headers.RetryAfter.Delta.Value;

        if (response.Headers.RetryAfter.Date.HasValue)
        {
            var delay = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
        }

        return null;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        clone.Version = request.Version;

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            ((IDictionary<string, object?>)clone.Options).Add(option.Key, option.Value);
        }

        return clone;
    }
}

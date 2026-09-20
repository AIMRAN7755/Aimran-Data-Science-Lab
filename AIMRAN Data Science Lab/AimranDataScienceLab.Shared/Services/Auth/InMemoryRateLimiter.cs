using System.Collections.Concurrent;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// In-memory sliding-window rate limiter that tracks request counts per key.
/// Default: 60 requests per minute per key.
/// </summary>
internal sealed class InMemoryRateLimiter : IRateLimiter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, RequestLog> _logs = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="maxRequests">Maximum requests allowed per window.</param>
    /// <param name="window">Sliding window duration.</param>
    public InMemoryRateLimiter(int maxRequests = 60, TimeSpan? window = null)
    {
        _maxRequests = maxRequests;
        _window = window ?? TimeSpan.FromMinutes(1);
    }

    public bool IsAllowed(string key)
    {
        var log = _logs.GetOrAdd(key, _ => new RequestLog());
        lock (log)
        {
            log.Prune(_window);
            if (log.Timestamps.Count >= _maxRequests)
                return false;

            log.Timestamps.Add(DateTime.UtcNow);
            return true;
        }
    }

    public int GetRemaining(string key)
    {
        if (!_logs.TryGetValue(key, out var log))
            return _maxRequests;

        lock (log)
        {
            log.Prune(_window);
            return Math.Max(0, _maxRequests - log.Timestamps.Count);
        }
    }

    private sealed class RequestLog
    {
        public List<DateTime> Timestamps { get; } = [];

        public void Prune(TimeSpan window)
        {
            var cutoff = DateTime.UtcNow - window;
            Timestamps.RemoveAll(t => t < cutoff);
        }
    }
}

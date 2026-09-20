namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Tracks and limits the rate of API calls per user/key to prevent abuse.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Check whether the given key is allowed to make a request.
    /// Returns true if under the limit, false if throttled.
    /// </summary>
    bool IsAllowed(string key);

    /// <summary>
    /// Get remaining requests allowed in the current window for a key.
    /// </summary>
    int GetRemaining(string key);
}

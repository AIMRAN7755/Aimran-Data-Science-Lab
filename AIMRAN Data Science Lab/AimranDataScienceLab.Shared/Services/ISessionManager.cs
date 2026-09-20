namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Tracks user activity and triggers auto-logout after inactivity.
/// </summary>
public interface ISessionManager
{
    /// <summary>Duration of inactivity before the session expires.</summary>
    TimeSpan Timeout { get; }

    /// <summary>Whether the current session has expired.</summary>
    bool IsExpired { get; }

    /// <summary>Record user activity to reset the inactivity timer.</summary>
    void RecordActivity();

    /// <summary>Start monitoring for session expiration.</summary>
    void Start();

    /// <summary>Stop monitoring (e.g., on logout).</summary>
    void Stop();

    /// <summary>Raised when the session times out due to inactivity.</summary>
    event Func<Task>? SessionExpired;
}

using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// Monitors inactivity and auto-logs out the user after a configurable timeout.
/// Default: 30 minutes.
/// </summary>
internal sealed class SessionManager : ISessionManager, IDisposable
{
    private readonly IAuthService _auth;
    private readonly ILogger<SessionManager> _logger;
    private readonly Timer _timer;

    private DateTime _lastActivity = DateTime.UtcNow;
    private bool _running;

    public TimeSpan Timeout { get; }

    public bool IsExpired => _running && DateTime.UtcNow - _lastActivity > Timeout;

    public event Func<Task>? SessionExpired;

    public SessionManager(
        IAuthService auth,
        ILogger<SessionManager> logger,
        TimeSpan? timeout = null)
    {
        _auth = auth;
        _logger = logger;
        Timeout = timeout ?? TimeSpan.FromMinutes(30);

        // Check every 30 seconds
        _timer = new Timer(CheckExpiration, null, System.Threading.Timeout.InfiniteTimeSpan, System.Threading.Timeout.InfiniteTimeSpan);

        // Auto-start/stop on auth state changes
        _auth.AuthStateChanged += OnAuthStateChanged;
    }

    public void RecordActivity() => _lastActivity = DateTime.UtcNow;

    public void Start()
    {
        _lastActivity = DateTime.UtcNow;
        _running = true;
        _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void Stop()
    {
        _running = false;
        _timer.Change(System.Threading.Timeout.InfiniteTimeSpan, System.Threading.Timeout.InfiniteTimeSpan);
    }

    private async void CheckExpiration(object? state)
    {
        if (!_running)
            return;

        var user = await _auth.GetCurrentUserAsync();
        if (user is null)
        {
            Stop();
            return;
        }

        if (!IsExpired)
            return;

        _logger.LogInformation("Session expired for {User} after {Timeout} inactivity",
            user.Username, Timeout);

        Stop();
        await _auth.LogoutAsync();

        if (SessionExpired is not null)
        {
            await SessionExpired.Invoke();
        }
    }

    private void OnAuthStateChanged()
    {
        // Start timer when user logs in, stop on logout
        var task = _auth.GetCurrentUserAsync();
        task.ContinueWith(t =>
        {
            if (t.Result is not null)
                Start();
            else
                Stop();
        }, TaskScheduler.Default);
    }

    public void Dispose()
    {
        _auth.AuthStateChanged -= OnAuthStateChanged;
        _timer.Dispose();
    }
}

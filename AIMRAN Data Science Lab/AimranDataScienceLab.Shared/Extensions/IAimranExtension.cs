namespace AIMRAN_Data_Science_Lab.Extensions;

/// <summary>
/// Base contract that every AIMRAN Data Science Lab extension must implement.
/// The extension host calls lifecycle methods in order:
/// <see cref="InitializeAsync"/> → <see cref="ActivateAsync"/> → (running) → <see cref="DeactivateAsync"/>.
/// </summary>
public interface IAimranExtension : IAsyncDisposable
{
    /// <summary>
    /// Unique extension identifier (must match manifest <c>id</c>).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called once after the assembly is loaded.
    /// Use this to register services, commands, and contributions with the host.
    /// </summary>
    Task InitializeAsync(IExtensionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the extension becomes active (user enables it or IDE starts).
    /// </summary>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the extension is being disabled or unloaded.
    /// Release resources and unregister contributions here.
    /// </summary>
    Task DeactivateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Runtime context the IDE host provides to each extension during initialization.
/// Gives extensions access to IDE services without coupling to internals.
/// </summary>
public interface IExtensionContext
{
    /// <summary>
    /// Execute a Python code snippet via the IDE's Python engine.
    /// </summary>
    Task<Services.CodeExecutionResult> ExecutePythonAsync(
        string code,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an R code snippet via the IDE's R engine.
    /// </summary>
    Task<Services.CodeExecutionResult> ExecuteRAsync(
        string code,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a command the extension exposes.
    /// </summary>
    void RegisterCommand(ExtensionCommand command);

    /// <summary>
    /// Log a message to the IDE's extension output channel.
    /// </summary>
    void Log(string message);

    /// <summary>
    /// Log an error to the IDE's extension output channel.
    /// </summary>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Show a notification toast in the IDE.
    /// </summary>
    void ShowNotification(string message, NotificationLevel level = NotificationLevel.Info);

    /// <summary>
    /// Navigate the IDE to a specific route.
    /// </summary>
    void NavigateTo(string route);

    /// <summary>
    /// Get the root directory where the extension is installed.
    /// </summary>
    string ExtensionDirectory { get; }
}

/// <summary>
/// A command an extension registers with the IDE.
/// </summary>
public sealed record ExtensionCommand
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Icon { get; init; }
    public string? KeyBinding { get; init; }

    /// <summary>
    /// Delegate invoked when the command is executed.
    /// </summary>
    public required Func<CancellationToken, Task> ExecuteAsync { get; init; }
}

/// <summary>
/// Notification severity levels.
/// </summary>
public enum NotificationLevel
{
    Info,
    Warning,
    Error,
    Success
}

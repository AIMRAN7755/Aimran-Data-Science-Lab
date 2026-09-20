using AIMRAN_Data_Science_Lab.Services;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab.Extensions;

/// <summary>
/// Default implementation of <see cref="IExtensionContext"/> provided to each
/// extension during initialization. Bridges extensions to IDE services.
/// </summary>
internal sealed class ExtensionContext : IExtensionContext
{
    private readonly IPythonExecutionService _pythonExecution;
    private readonly IRExecutionService _rExecution;
    private readonly ILogger _logger;
    private readonly List<ExtensionCommand> _commands = [];

    public ExtensionContext(
        string extensionDirectory,
        IPythonExecutionService pythonExecution,
        IRExecutionService rExecution,
        ILogger logger)
    {
        ExtensionDirectory = extensionDirectory;
        _pythonExecution = pythonExecution;
        _rExecution = rExecution;
        _logger = logger;
    }

    public string ExtensionDirectory { get; }

    public async Task<CodeExecutionResult> ExecutePythonAsync(
        string code,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        return await _pythonExecution.ExecuteAsync(code, timeoutSeconds, cancellationToken: cancellationToken);
    }

    public async Task<CodeExecutionResult> ExecuteRAsync(
        string code,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        return await _rExecution.ExecuteAsync(code, timeoutSeconds, cancellationToken: cancellationToken);
    }

    public void RegisterCommand(ExtensionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands.Add(command);
        _logger.LogInformation("Extension command registered: {CommandId}", command.Id);
    }

    /// <summary>
    /// Returns all commands registered via this context.
    /// </summary>
    internal IReadOnlyList<ExtensionCommand> RegisteredCommands => _commands;

    public void Log(string message) =>
        _logger.LogInformation("[Extension] {Message}", message);

    public void LogError(string message, Exception? exception = null) =>
        _logger.LogError(exception, "[Extension] {Message}", message);

    public void ShowNotification(string message, NotificationLevel level = NotificationLevel.Info) =>
        _logger.LogInformation("[Notification:{Level}] {Message}", level, message);

    public void NavigateTo(string route) =>
        _logger.LogInformation("[Navigation] {Route}", route);
}

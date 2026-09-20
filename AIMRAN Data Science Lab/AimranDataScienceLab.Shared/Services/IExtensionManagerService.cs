using AIMRAN_Data_Science_Lab.Extensions;
using AIMRAN_Data_Science_Lab.Models.Extensions;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Manages the full lifecycle of IDE extensions: discovery, loading,
/// activation, deactivation, and uninstallation.
/// </summary>
public interface IExtensionManagerService
{
    /// <summary>
    /// Scan the extensions directory and discover all available extensions.
    /// </summary>
    Task<IReadOnlyList<ExtensionInfo>> DiscoverExtensionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all currently known extensions and their states.
    /// </summary>
    IReadOnlyList<ExtensionInfo> GetAllExtensions();

    /// <summary>
    /// Get a single extension by its manifest ID.
    /// </summary>
    ExtensionInfo? GetExtension(string extensionId);

    /// <summary>
    /// Load and activate an extension. Calls <see cref="IAimranExtension.InitializeAsync"/>
    /// then <see cref="IAimranExtension.ActivateAsync"/>.
    /// </summary>
    Task<ExtensionInfo> EnableExtensionAsync(string extensionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate and unload an extension.
    /// </summary>
    Task<ExtensionInfo> DisableExtensionAsync(string extensionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Install an extension from a directory or package path.
    /// Copies the extension into the extensions folder and discovers it.
    /// </summary>
    Task<ExtensionInfo> InstallExtensionAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove an extension from the extensions directory permanently.
    /// </summary>
    Task UninstallExtensionAsync(string extensionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all commands registered by active extensions.
    /// </summary>
    IReadOnlyList<ExtensionCommand> GetRegisteredCommands();

    /// <summary>
    /// Execute a registered extension command by ID.
    /// </summary>
    Task ExecuteCommandAsync(string commandId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when an extension's state changes.
    /// </summary>
    event EventHandler<ExtensionStateChangedEventArgs>? ExtensionStateChanged;
}

/// <summary>
/// Event args for extension state changes.
/// </summary>
public sealed record ExtensionStateChangedEventArgs(string ExtensionId, ExtensionState NewState);

using System.Text.Json.Serialization;

namespace AIMRAN_Data_Science_Lab.Models.Extensions;

/// <summary>
/// Describes an extension package loaded from its manifest.json file.
/// </summary>
public sealed record ExtensionManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Author { get; init; }
    public string? Description { get; init; }
    public string? IconPath { get; init; }
    public ExtensionCategory Category { get; init; } = ExtensionCategory.Tool;

    /// <summary>
    /// The .dll entry point relative to the extension directory.
    /// </summary>
    public required string EntryAssembly { get; init; }

    /// <summary>
    /// Minimum IDE version required.
    /// </summary>
    public string? MinIdeVersion { get; init; }

    /// <summary>
    /// Permissions the extension requests from the host.
    /// </summary>
    public IReadOnlyList<ExtensionPermission> Permissions { get; init; } = [];

    /// <summary>
    /// UI contribution points this extension registers.
    /// </summary>
    public ExtensionContributions Contributions { get; init; } = new();
}

/// <summary>
/// Runtime state of a discovered extension.
/// </summary>
public sealed record ExtensionInfo
{
    public required ExtensionManifest Manifest { get; init; }
    public required string DirectoryPath { get; init; }
    public ExtensionState State { get; init; } = ExtensionState.Discovered;
    public string? ErrorMessage { get; init; }
    public DateTime? ActivatedAt { get; init; }
}

/// <summary>
/// Lifecycle state of an extension.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExtensionState
{
    Discovered,
    Loading,
    Active,
    Disabled,
    Error
}

/// <summary>
/// High-level category for an extension.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExtensionCategory
{
    Language,
    DataScience,
    Visualization,
    DataConnector,
    AI,
    Tool,
    Theme
}

/// <summary>
/// Permissions an extension can request.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExtensionPermission
{
    FileSystem,
    Network,
    DatasetAccess,
    ModelAccess,
    MlTraining,
    CodeExecution,
    SystemResources
}

/// <summary>
/// Declares UI contribution points the extension injects into the IDE.
/// </summary>
public sealed record ExtensionContributions
{
    /// <summary>
    /// Commands the extension registers (appear in command palette / toolbars).
    /// </summary>
    public IReadOnlyList<CommandContributionDescriptor> Commands { get; init; } = [];

    /// <summary>
    /// Sidebar panels the extension contributes.
    /// </summary>
    public IReadOnlyList<SidebarContributionDescriptor> SidebarPanels { get; init; } = [];

    /// <summary>
    /// Menu items injected into IDE menus.
    /// </summary>
    public IReadOnlyList<MenuItemDescriptor> MenuItems { get; init; } = [];
}

/// <summary>
/// Describes a command contribution from an extension manifest.
/// </summary>
public sealed record CommandContributionDescriptor
{
    public required string CommandId { get; init; }
    public required string Title { get; init; }
    public string? Icon { get; init; }
    public string? KeyBinding { get; init; }
}

/// <summary>
/// Describes a sidebar panel contribution from an extension manifest.
/// </summary>
public sealed record SidebarContributionDescriptor
{
    public required string PanelId { get; init; }
    public required string Title { get; init; }
    public string? Icon { get; init; }

    /// <summary>
    /// Fully qualified Blazor component type name to render in the sidebar.
    /// </summary>
    public string? ComponentType { get; init; }
}

/// <summary>
/// Describes a menu item injected by an extension.
/// </summary>
public sealed record MenuItemDescriptor
{
    public required string MenuId { get; init; }
    public required string Label { get; init; }
    public string? Icon { get; init; }

    /// <summary>
    /// Navigation route or command ID to invoke.
    /// </summary>
    public string? Action { get; init; }
}

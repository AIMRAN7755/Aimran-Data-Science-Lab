namespace AIMRAN_Data_Science_Lab.Models;

/// <summary>
/// Represents a data science project in the DS-Workbench.
/// </summary>
public record Project
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string LocalPath { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
    public ProjectStatus Status { get; init; } = ProjectStatus.Active;
    public IReadOnlyList<Guid> DatasetIds { get; init; } = [];
    public IReadOnlyList<Guid> ExperimentIds { get; init; } = [];
    public IReadOnlyList<Guid> ModelIds { get; init; } = [];
    public AzureProjectConfig? AzureConfig { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public enum ProjectStatus
{
    Active,
    Archived,
    Completed
}

/// <summary>
/// Azure-specific configuration for a project.
/// </summary>
public record AzureProjectConfig
{
    public required string SubscriptionId { get; init; }
    public required string ResourceGroupName { get; init; }
    public required string WorkspaceName { get; init; }
    public string? StorageAccountName { get; init; }
    public string? ContainerName { get; init; }
    public bool IsConnected { get; init; }
}

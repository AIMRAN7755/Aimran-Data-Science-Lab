namespace AIMRAN_Data_Science_Lab.Models;

/// <summary>
/// Represents a trained machine learning model in the DS-Workbench.
/// </summary>
public record MlModel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid ExperimentId { get; init; }
    public required string Algorithm { get; init; }
    public required string Framework { get; init; }
    public int Version { get; init; } = 1;
    public required string FilePath { get; init; }
    public long SizeBytes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public ModelStatus Status { get; init; } = ModelStatus.Draft;
    public IReadOnlyDictionary<string, double> PerformanceMetrics { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, object> Hyperparameters { get; init; } = new Dictionary<string, object>();
    public string? AzureMlModelId { get; init; }
    public bool IsDeployed { get; init; }
    public string? DeploymentEndpoint { get; init; }
}

public enum ModelStatus
{
    Draft,
    Training,
    Trained,
    Validated,
    Registered,
    Deployed,
    Archived
}

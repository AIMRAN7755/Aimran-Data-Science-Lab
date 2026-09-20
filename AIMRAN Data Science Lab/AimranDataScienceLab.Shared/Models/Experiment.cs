namespace AIMRAN_Data_Science_Lab.Models;

/// <summary>
/// Represents a machine learning experiment in the DS-Workbench.
/// </summary>
public record Experiment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid ProjectId { get; init; }
    public Guid DatasetId { get; init; }
    public ExperimentStatus Status { get; init; } = ExperimentStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public ComputeTarget ComputeTarget { get; init; } = ComputeTarget.Local;
    public IReadOnlyDictionary<string, object> Hyperparameters { get; init; } = new Dictionary<string, object>();
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
    public IReadOnlyList<ExperimentRun> Runs { get; init; } = [];
    public string? AzureMlExperimentId { get; init; }
}

public record ExperimentRun
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int RunNumber { get; init; }
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; init; }
    public ExperimentStatus Status { get; init; }
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
    public string? LogOutput { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum ExperimentStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum ComputeTarget
{
    Local,
    AzureMl,
    AzureGpu
}

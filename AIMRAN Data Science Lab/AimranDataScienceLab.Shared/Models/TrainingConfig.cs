namespace AIMRAN_Data_Science_Lab.Models;

/// <summary>
/// Configuration for a model training run within a project workspace.
/// </summary>
public record TrainingConfig
{
    public string PythonEnvironment { get; init; } = "default";
    public Guid DatasetId { get; init; }
    public string Algorithm { get; init; } = "";
    public string Framework { get; init; } = "";
    public ComputeTarget ComputeTarget { get; init; } = ComputeTarget.Local;
    public int Epochs { get; init; } = 10;
    public int BatchSize { get; init; } = 32;
    public double LearningRate { get; init; } = 0.001;
    public double ValidationSplit { get; init; } = 0.2;
    public int? RandomSeed { get; init; } = 42;
    public IReadOnlyDictionary<string, object> ExtraHyperparameters { get; init; } = new Dictionary<string, object>();
}

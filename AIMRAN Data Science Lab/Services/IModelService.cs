using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing ML models in the DS-Workbench.
/// </summary>
public interface IModelService
{
    Task<IReadOnlyList<MlModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MlModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MlModel> RegisterAsync(string name, Guid experimentId, string algorithm, string framework, string filePath, IDictionary<string, double>? metrics = null, CancellationToken cancellationToken = default);
    Task<MlModel> CreateVersionAsync(Guid modelId, string newFilePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ModelTestResult> TestAsync(Guid modelId, Guid testDatasetId, CancellationToken cancellationToken = default);
    Task<MlModel> DeployAsync(Guid modelId, string endpoint, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of testing a model.
/// </summary>
public record ModelTestResult
{
    public Guid ModelId { get; init; }
    public Guid DatasetId { get; init; }
    public DateTime TestedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
    public string? Summary { get; init; }
}

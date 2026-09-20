using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local in-memory implementation of the model service.
/// </summary>
public sealed class LocalModelService : IModelService
{
    private readonly List<MlModel> _models = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<MlModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<MlModel>>(_models.ToList());
        }
    }

    public Task<MlModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_models.FirstOrDefault(m => m.Id == id));
        }
    }

    public Task<MlModel> RegisterAsync(
        string name,
        Guid experimentId,
        string algorithm,
        string framework,
        string filePath,
        IDictionary<string, double>? metrics = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(framework);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fileInfo = new FileInfo(filePath);
        var metricsDict = metrics != null 
            ? new Dictionary<string, double>(metrics).AsReadOnly() 
            : new Dictionary<string, double>().AsReadOnly();
        var model = new MlModel
        {
            Name = name,
            ExperimentId = experimentId,
            Algorithm = algorithm,
            Framework = framework,
            FilePath = filePath,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Status = ModelStatus.Registered,
            PerformanceMetrics = metricsDict,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _models.Add(model);
        }

        return Task.FromResult(model);
    }

    public async Task<MlModel> CreateVersionAsync(Guid modelId, string newFilePath, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(modelId, cancellationToken)
            ?? throw new InvalidOperationException($"Model {modelId} not found.");

        var fileInfo = new FileInfo(newFilePath);
        var newModel = existing with
        {
            Id = Guid.NewGuid(),
            FilePath = newFilePath,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Version = existing.Version + 1,
            Status = ModelStatus.Registered,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _models.Add(newModel);
        }

        return newModel;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _models.RemoveAll(m => m.Id == id);
        }
        return Task.CompletedTask;
    }

    public Task<ModelTestResult> TestAsync(Guid modelId, Guid testDatasetId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Placeholder - real implementation would load model and run predictions
        var result = new ModelTestResult
        {
            ModelId = modelId,
            DatasetId = testDatasetId,
            TestedAt = DateTime.UtcNow,
            Metrics = new Dictionary<string, double>
            {
                ["accuracy"] = 0.0,
                ["precision"] = 0.0,
                ["recall"] = 0.0,
                ["f1_score"] = 0.0
            },
            Summary = "Test placeholder - connect to ML runtime for actual testing."
        };

        return Task.FromResult(result);
    }

    public Task<MlModel> DeployAsync(Guid modelId, string endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var index = _models.FindIndex(m => m.Id == modelId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Model {modelId} not found.");
            }

            var updated = _models[index] with
            {
                Status = ModelStatus.Deployed,
                IsDeployed = true,
                DeploymentEndpoint = endpoint
            };

            _models[index] = updated;
            return Task.FromResult(updated);
        }
    }
}

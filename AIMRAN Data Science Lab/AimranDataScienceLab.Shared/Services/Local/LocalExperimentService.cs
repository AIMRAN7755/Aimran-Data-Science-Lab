using System.Runtime.CompilerServices;
using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local in-memory implementation of the experiment service.
/// </summary>
public sealed class LocalExperimentService : IExperimentService
{
    private readonly List<Experiment> _experiments = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<Experiment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Experiment>>(_experiments.ToList());
        }
    }

    public Task<IReadOnlyList<Experiment>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Experiment>>(
                _experiments.Where(e => e.ProjectId == projectId).ToList());
        }
    }

    public Task<Experiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_experiments.FirstOrDefault(e => e.Id == id));
        }
    }

    public Task<Experiment> CreateAsync(string name, Guid projectId, Guid datasetId, string? description = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        var experiment = new Experiment
        {
            Name = name,
            Description = description,
            ProjectId = projectId,
            DatasetId = datasetId,
            Status = ExperimentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _experiments.Add(experiment);
        }

        return Task.FromResult(experiment);
    }

    public Task<Experiment> StartAsync(Guid experimentId, IDictionary<string, object>? hyperparameters = null, ComputeTarget target = ComputeTarget.Local, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var index = _experiments.FindIndex(e => e.Id == experimentId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Experiment {experimentId} not found.");
            }

            var hyperparamsDict = hyperparameters != null 
                ? new Dictionary<string, object>(hyperparameters).AsReadOnly() 
                : new Dictionary<string, object>().AsReadOnly();
            var updated = _experiments[index] with
            {
                Status = ExperimentStatus.Running,
                StartedAt = DateTime.UtcNow,
                ComputeTarget = target,
                Hyperparameters = hyperparamsDict
            };

            _experiments[index] = updated;
            return Task.FromResult(updated);
        }
    }

    public Task<Experiment> StopAsync(Guid experimentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var index = _experiments.FindIndex(e => e.Id == experimentId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Experiment {experimentId} not found.");
            }

            var updated = _experiments[index] with
            {
                Status = ExperimentStatus.Cancelled,
                CompletedAt = DateTime.UtcNow
            };

            _experiments[index] = updated;
            return Task.FromResult(updated);
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _experiments.RemoveAll(e => e.Id == id);
        }
        return Task.CompletedTask;
    }

    public Task<Experiment> LogMetricsAsync(Guid experimentId, IDictionary<string, double> metrics, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var index = _experiments.FindIndex(e => e.Id == experimentId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Experiment {experimentId} not found.");
            }

            var existing = _experiments[index];
            var mergedMetrics = existing.Metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            foreach (var (key, value) in metrics)
            {
                mergedMetrics[key] = value;
            }

            var updated = existing with { Metrics = mergedMetrics.AsReadOnly() };
            _experiments[index] = updated;
            return Task.FromResult(updated);
        }
    }

    public async IAsyncEnumerable<ExperimentRun> StreamRunsAsync(Guid experimentId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var experiment = await GetByIdAsync(experimentId, cancellationToken);
        if (experiment is null)
        {
            yield break;
        }

        foreach (var run in experiment.Runs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return run;
        }
    }
}

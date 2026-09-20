using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing experiments in the DS-Workbench.
/// </summary>
public interface IExperimentService
{
    Task<IReadOnlyList<Experiment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Experiment>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Experiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Experiment> CreateAsync(string name, Guid projectId, Guid datasetId, string? description = null, CancellationToken cancellationToken = default);
    Task<Experiment> StartAsync(Guid experimentId, IDictionary<string, object>? hyperparameters = null, ComputeTarget target = ComputeTarget.Local, CancellationToken cancellationToken = default);
    Task<Experiment> StopAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Experiment> LogMetricsAsync(Guid experimentId, IDictionary<string, double> metrics, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ExperimentRun> StreamRunsAsync(Guid experimentId, CancellationToken cancellationToken = default);
}

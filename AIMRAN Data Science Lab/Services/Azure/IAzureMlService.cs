using AIMRAN_Data_Science_Lab.Models;
using AIMRAN_Data_Science_Lab.Models.Azure;

namespace AIMRAN_Data_Science_Lab.Services.Azure;

/// <summary>
/// Service for Azure Machine Learning operations.
/// </summary>
public interface IAzureMlService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task<AzureOperationResult> ConfigureAsync(AzureMlConfig config, CancellationToken cancellationToken = default);
    Task<AzureOperationResult> SubmitExperimentAsync(Experiment experiment, string scriptPath, CancellationToken cancellationToken = default);
    Task<Experiment> GetExperimentStatusAsync(string azureExperimentId, CancellationToken cancellationToken = default);
    Task<AzureOperationResult> CancelExperimentAsync(string azureExperimentId, CancellationToken cancellationToken = default);
    Task<AzureOperationResult> RegisterModelAsync(MlModel model, CancellationToken cancellationToken = default);
    Task<AzureOperationResult> DeployModelAsync(string azureModelId, string endpointName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAvailableComputeTargetsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<ExperimentRun> StreamExperimentLogsAsync(string azureExperimentId, CancellationToken cancellationToken = default);
}

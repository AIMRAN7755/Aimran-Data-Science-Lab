using AIMRAN_Data_Science_Lab.Services;
using AIMRAN_Data_Science_Lab.Services.Azure;
using Microsoft.Extensions.Options;

namespace AimranDataScienceLab.Engine;

internal sealed class AimranEngine : IAimranEngine
{
    private readonly EngineOptions _options;

    public AimranEngine(
        IOptions<EngineOptions> options,
        IProjectService projects,
        IDatasetService datasets,
        IExperimentService experiments,
        IModelService models,
        IResourceMonitorService resources,
        IDataProfilingService dataProfiling,
        IOutlierDetectionService outliers,
        IDataCleaningService dataCleaning,
        ICleaningRuleService cleaningRules,
        IDatasetVersionService datasetVersioning,
        IAzureConfigService azureConfig,
        IAzureStorageService azureStorage,
        IAzureMlService azureMl)
    {
        _options = options.Value;

        Projects = projects;
        Datasets = datasets;
        Experiments = experiments;
        Models = models;
        Resources = resources;

        DataProfiling = dataProfiling;
        Outliers = outliers;
        DataCleaning = dataCleaning;
        CleaningRules = cleaningRules;

        DatasetVersioning = datasetVersioning;

        AzureConfig = azureConfig;
        AzureStorage = azureStorage;
        AzureML = azureMl;
    }

    public EngineOptions Options => _options;

    public IProjectService Projects { get; }

    public IDatasetService Datasets { get; }

    public IExperimentService Experiments { get; }

    public IModelService Models { get; }

    public IResourceMonitorService Resources { get; }

    public IDataProfilingService DataProfiling { get; }

    public IOutlierDetectionService Outliers { get; }

    public IDataCleaningService DataCleaning { get; }

    public ICleaningRuleService CleaningRules { get; }

    public IDatasetVersionService DatasetVersioning { get; }

    public IAzureConfigService AzureConfig { get; }

    public IAzureStorageService AzureStorage { get; }

    public IAzureMlService AzureML { get; }

    public async Task<EngineResult<EngineSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectsTask = Projects.GetAllAsync(cancellationToken);
            var datasetsTask = Datasets.GetAllAsync(cancellationToken);
            var experimentsTask = Experiments.GetAllAsync(cancellationToken);
            var modelsTask = Models.GetAllAsync(cancellationToken);
            var metricsTask = Resources.GetCurrentMetricsAsync(cancellationToken);
            var azureConfiguredTask = AzureConfig.ValidateConnectionAsync(cancellationToken);

            await Task.WhenAll(projectsTask, datasetsTask, experimentsTask, modelsTask, metricsTask, azureConfiguredTask);

            var snapshot = new EngineSnapshot
            {
                ProjectCount = (await projectsTask).Count,
                DatasetCount = (await datasetsTask).Count,
                ExperimentCount = (await experimentsTask).Count,
                ModelCount = (await modelsTask).Count,
                ResourceMetrics = await metricsTask,
                AzureConfigured = _options.EnableAzureFeatures && await azureConfiguredTask,
                AzureAuthenticated = _options.EnableAzureFeatures && AzureConfig.IsAuthenticated
            };

            return EngineResult<EngineSnapshot>.Ok(snapshot);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            return EngineResult<EngineSnapshot>.Fail(EngineErrorCode.Unauthorized, ex.Message);
        }
        catch (IOException ex)
        {
            return EngineResult<EngineSnapshot>.Fail(EngineErrorCode.IoFailure, ex.Message);
        }
        catch (Exception ex)
        {
            return EngineResult<EngineSnapshot>.Fail(EngineErrorCode.InternalError, ex.Message);
        }
    }

    public async Task<EngineResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_options.EnableAzureFeatures)
            {
                _ = await AzureConfig.GetConfigAsync(cancellationToken);
            }

            return EngineResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            return EngineResult.Fail(EngineErrorCode.Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            return EngineResult.Fail(EngineErrorCode.InternalError, ex.Message);
        }
    }
}

using AIMRAN_Data_Science_Lab.Services;
using AIMRAN_Data_Science_Lab.Services.Azure;
using AIMRAN_Data_Science_Lab.Models;

namespace AimranDataScienceLab.Engine;

/// <summary>
/// Professional façade API that composes AIMRAN Data Science Lab capabilities.
/// Treat this as the main entrypoint for orchestrating higher-level workflows.
/// </summary>
public interface IAimranEngine
{
    EngineOptions Options { get; }

    IProjectService Projects { get; }

    IDatasetService Datasets { get; }

    IExperimentService Experiments { get; }

    IModelService Models { get; }

    IResourceMonitorService Resources { get; }

    IDataProfilingService DataProfiling { get; }

    IOutlierDetectionService Outliers { get; }

    IDataCleaningService DataCleaning { get; }

    ICleaningRuleService CleaningRules { get; }

    IDatasetVersionService DatasetVersioning { get; }

    IAzureConfigService AzureConfig { get; }

    IAzureStorageService AzureStorage { get; }

    IAzureMlService AzureML { get; }

    Task<EngineResult<EngineSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<EngineResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

public sealed record EngineSnapshot
{
    public int ProjectCount { get; init; }
    public int DatasetCount { get; init; }
    public int ExperimentCount { get; init; }
    public int ModelCount { get; init; }

    public ResourceMetrics? ResourceMetrics { get; init; }

    public bool AzureConfigured { get; init; }
    public bool AzureAuthenticated { get; init; }
}

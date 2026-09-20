using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.MachineLearning;
using AimranDataScienceLab.Core.Domain.AI;

namespace AimranDataScienceLab.Core.Services.MachineLearning;

/// <summary>
/// ML Studio Service for Data Scientists.
/// Provides model training, evaluation, and management capabilities.
/// </summary>
public interface IMLStudioService
{
    Task<MLModel> CreateModelAsync(MLModel model);
    Task<MLModel?> GetModelAsync(Guid modelId);
    Task<MLModel[]> GetProjectModelsAsync(Guid projectId);
    Task<ModelVersion> TrainModelAsync(Guid modelId, TrainingRequest request);
    Task<ModelPerformance> EvaluateModelAsync(Guid modelVersionId);
    Task<ModelVersion[]> GetModelVersionsAsync(Guid modelId);
}

public class TrainingRequest
{
    public Guid DatasetId { get; set; }
    public string TargetVariable { get; set; } = string.Empty;
    public string[] FeatureNames { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Hyperparameters { get; set; } = new();
    public double? ValidationSplit { get; set; } = 0.2;
    public int? RandomState { get; set; }
    public string[]? PreprocessingSteps { get; set; }
}

/// <summary>
/// Feature Engineering Service for Data Scientists.
/// Handles feature creation, transformation, and selection.
/// </summary>
public interface IFeatureEngineeringService
{
    Task<FeatureSet> CreateFeatureSetAsync(FeatureSet featureSet);
    Task<FeatureSet?> GetFeatureSetAsync(Guid featureSetId);
    Task<FeatureSet[]> GetProjectFeatureSetsAsync(Guid projectId);

    Task<Feature> CreateFeatureAsync(Feature feature);
    Task<Feature[]> GetDatasetFeaturesAsync(Guid datasetId);
    Task<FeatureImportance[]> AnalyzeFeatureImportanceAsync(Guid modelVersionId);

    Task<FeatureSet> SelectBestFeaturesAsync(Guid featureSetId, SelectionMethod method);
    Task<FeatureSet> NormalizeFeatureSetAsync(Guid featureSetId, NormalizationMethod method);
}

public class FeatureSet
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DatasetId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid[] FeatureIds { get; set; } = Array.Empty<Guid>();
    public FeatureEngineeringStep[] Steps { get; set; } = Array.Empty<FeatureEngineeringStep>();

    public int FeatureCount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Feature
{
    public Guid Id { get; set; }
    public Guid DatasetId { get; set; }
    public Guid FeatureSetId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public FeatureType Type { get; set; }

    public string? Description { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public DateTime CreatedAt { get; set; }
}

public enum FeatureType
{
    Numerical,
    Categorical,
    Text,
    DateTime,
    Binary,
    Ordinal,
    Derived
}

public class FeatureEngineeringStep
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Encoding, Scaling, Creation, Selection
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int Order { get; set; }
    public bool IsApplied { get; set; }
}

public class FeatureImportance
{
    public string FeatureName { get; set; } = string.Empty;
    public double ImportanceScore { get; set; }
    public string ExplanationType { get; set; } = string.Empty; // SHAP, Permutation, TreeBased
}

public enum SelectionMethod
{
    Univariate,
    RecursiveElimination,
    LassoRegularization,
    TreeBased,
    CorrelationBased
}

public enum NormalizationMethod
{
    StandardScaling,
    MinMaxScaling,
    RobustScaling,
    LogTransform
}

/// <summary>
/// Experiment Tracker for Data Scientists.
/// Tracks model experiments, hyperparameters, and results.
/// </summary>
public interface IExperimentTrackerService
{
    Task<ModelExperiment> StartExperimentAsync(ModelExperiment experiment);
    Task<ModelExperiment?> GetExperimentAsync(Guid experimentId);
    Task<ModelExperiment[]> GetProjectExperimentsAsync(Guid projectId);
    Task<ModelExperiment[]> GetModelExperimentsAsync(Guid modelId);

    Task UpdateExperimentResultsAsync(Guid experimentId, ModelPerformance results);
    Task CompleteExperimentAsync(Guid experimentId);
    Task CancelExperimentAsync(Guid experimentId);

    Task<ExperimentComparison> CompareExperimentsAsync(Guid[] experimentIds);
    Task<ModelExperiment> GetBestExperimentAsync(Guid modelId);
}

public class ExperimentComparison
{
    public ModelExperiment[] Experiments { get; set; } = Array.Empty<ModelExperiment>();
    public Dictionary<string, double[]> MetricsComparison { get; set; } = new();
    public Dictionary<string, object[]> HyperparameterComparison { get; set; } = new();
    public Guid WinnerExperimentId { get; set; }
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Model Registry Service for Data Scientists.
/// Manages model versioning, metadata, and lineage.
/// </summary>
public interface IModelRegistryService
{
    Task<MLModel> RegisterModelAsync(MLModel model);
    Task<MLModel?> GetModelAsync(Guid modelId);
    Task<ModelVersion> RegisterVersionAsync(ModelVersion version);
    Task<ModelVersion?> GetVersionAsync(Guid versionId);

    Task PromoteVersionToProductionAsync(Guid modelVersionId);
    Task RollbackToVersionAsync(Guid modelId, int versionNumber);

    Task<ModelComparisonReport> CompareVersionsAsync(Guid version1Id, Guid version2Id);
    Task SetModelMetadataAsync(Guid modelId, Dictionary<string, object> metadata);
}

public class ModelComparisonReport
{
    public ModelVersion Version1 { get; set; } = new();
    public ModelVersion Version2 { get; set; } = new();

    public Dictionary<string, double> MetricsDifference { get; set; } = new();
    public bool Version2IsBetter { get; set; }
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// AutoML Service for automated model building.
/// </summary>
public interface IAutoMLService
{
    Task<AutoMLRun> RunAutoMLAsync(AutoMLConfiguration config);
    Task<AutoMLRun?> GetAutoMLRunAsync(Guid runId);
    Task<MLModel> GetBestAutoMLModelAsync(Guid runId);
    Task CancelAutoMLRunAsync(Guid runId);
}

public class AutoMLRun
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ConfigurationId { get; set; }

    public AutoMLStatus Status { get; set; }
    public int ModelsGenerated { get; set; }
    public int ModelsCompleted { get; set; }

    public MLModel? BestModel { get; set; }
    public ModelPerformance? BestPerformance { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan ElapsedTime => (CompletedAt ?? DateTime.UtcNow) - StartedAt;

    public string? ErrorMessage { get; set; }
    public string[] GeneratedModelIds { get; set; } = Array.Empty<string>();
}

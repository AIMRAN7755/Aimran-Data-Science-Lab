using System;
using System.Collections.Generic;

namespace AimranDataScienceLab.Core.Domain.MachineLearning;

/// <summary>
/// Represents a machine learning model in the registry.
/// Shared across Data Scientists and AI Engineers.
/// </summary>
public class MLModel
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ModelType Type { get; set; }
    public ModelFramework Framework { get; set; }

    public Guid TrainingDatasetId { get; set; }
    public string[] FeatureNames { get; set; } = Array.Empty<string>();
    public string TargetVariable { get; set; } = string.Empty;

    public Guid[] VersionIds { get; set; } = Array.Empty<Guid>();
    public Guid CurrentVersionId { get; set; }

    public ModelPerformance Performance { get; set; } = new();
    public ModelExplainability Explainability { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum ModelType
{
    Classification,
    Regression,
    Clustering,
    TimeSeries,
    NLP,
    ComputerVision,
    DeepLearning,
    Ensemble,
    Custom
}

public enum ModelFramework
{
    Scikit_Learn,
    TensorFlow,
    PyTorch,
    XGBoost,
    LightGBM,
    Statsmodels,
    H2O,
    MLFlow,
    Custom
}

/// <summary>
/// Model version with full reproducibility.
/// </summary>
public class ModelVersion
{
    public Guid Id { get; set; }
    public Guid ModelId { get; set; }
    public int VersionNumber { get; set; }
    public string Description { get; set; } = string.Empty;

    public string ArtifactPath { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = string.Empty;

    public ModelPerformance Performance { get; set; } = new();
    public TrainingMetadata Training { get; set; } = new();

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Comprehensive model performance metrics.
/// </summary>
public class ModelPerformance
{
    // Classification
    public double? Accuracy { get; set; }
    public double? Precision { get; set; }
    public double? Recall { get; set; }
    public double? F1Score { get; set; }
    public double? AUC_ROC { get; set; }

    // Regression
    public double? MAE { get; set; }
    public double? RMSE { get; set; }
    public double? R2Score { get; set; }

    // General
    public double? LogLoss { get; set; }
    public int? TrainingTimeSeconds { get; set; }
    public int? InferenceTimeMs { get; set; }

    public Dictionary<string, double> CustomMetrics { get; set; } = new();
    public DateTime? EvaluatedAt { get; set; }
}

public class TrainingMetadata
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int EpochsCompleted { get; set; }
    public string FrameworkVersion { get; set; } = string.Empty;
    public Dictionary<string, object> Hyperparameters { get; set; } = new();
    public string[] DataPreprocessingSteps { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Model explainability and interpretability.
/// </summary>
public class ModelExplainability
{
    public string[] FeatureImportance { get; set; } = Array.Empty<string>();
    public Dictionary<string, double> ImportanceScores { get; set; } = new();
    public string? SHAPAnalysis { get; set; }
    public string? LIMEExplanation { get; set; }
    public string? PartialDependencePlot { get; set; }
}

/// <summary>
/// Experimentation tracking for model iterations.
/// Tracks different model versions, hyperparameters, and results.
/// </summary>
public class ModelExperiment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid BaseModelId { get; set; }

    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Hyperparameters { get; set; } = new();

    public ModelPerformance Results { get; set; } = new();
    public ExperimentStatus Status { get; set; } = ExperimentStatus.Planned;

    public string[]? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ExperimentStatus
{
    Planned,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Model deployment configuration.
/// Used by AI Engineers for model serving.
/// </summary>
public class ModelDeployment
{
    public Guid Id { get; set; }
    public Guid ModelVersionId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DeploymentEnvironment Environment { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Draft;

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public ModelServingConfig ServingConfig { get; set; } = new();
    public MonitoringConfig Monitoring { get; set; } = new();

    public DeploymentMetrics Metrics { get; set; } = new();

    public Guid DeployedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeployedAt { get; set; }
}

public enum DeploymentEnvironment
{
    Development,
    Staging,
    Production,
    EdgeDevice
}

public enum DeploymentStatus
{
    Draft,
    Approved,
    Deploying,
    Active,
    Paused,
    Rollback,
    Retired
}

public class ModelServingConfig
{
    public int? ReplicaCount { get; set; }
    public int? MaxConcurrentRequests { get; set; }
    public int? MemoryMB { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? ContainerImage { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

public class MonitoringConfig
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public bool EnableAlerts { get; set; } = true;
    public double? DriftThreshold { get; set; } = 0.05;
    public int? CheckIntervalMinutes { get; set; } = 60;
}

public class DeploymentMetrics
{
    public int TotalRequests { get; set; }
    public double AverageLatencyMs { get; set; }
    public double ErrorRate { get; set; }
    public double? ModelDrift { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}

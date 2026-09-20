using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.MachineLearning;

namespace AimranDataScienceLab.Core.Services.Deployment;

/// <summary>
/// Deployment Center Service for AI Engineers.
/// Manages model deployment across multiple environments.
/// </summary>
public interface IDeploymentCenterService
{
    Task<ModelDeployment> CreateDeploymentAsync(ModelDeployment deployment);
    Task<ModelDeployment?> GetDeploymentAsync(Guid deploymentId);
    Task<ModelDeployment[]> GetProjectDeploymentsAsync(Guid projectId);

    Task<ModelDeployment> ApproveDeploymentAsync(Guid deploymentId);
    Task<ModelDeployment> DeployModelAsync(Guid deploymentId);
    Task<ModelDeployment> RollbackDeploymentAsync(Guid deploymentId);
    Task<ModelDeployment> PauseDeploymentAsync(Guid deploymentId);

    Task<DeploymentMetrics> GetDeploymentMetricsAsync(Guid deploymentId);
}

/// <summary>
/// API Manager Service for AI Engineers.
/// Manages model serving APIs and endpoints.
/// </summary>
public interface IAPIManagerService
{
    Task<ModelAPI> CreateAPIEndpointAsync(ModelAPI api);
    Task<ModelAPI?> GetAPIAsync(Guid apiId);
    Task<ModelAPI[]> GetDeploymentAPIsAsync(Guid deploymentId);

    Task UpdateAPIConfigAsync(Guid apiId, APIConfiguration config);
    Task<APIKey> GenerateAPIKeyAsync(Guid apiId);
    Task RevokeAPIKeyAsync(Guid apiKeyId);

    Task<APIStats> GetAPIStatsAsync(Guid apiId);
}

public class ModelAPI
{
    public Guid Id { get; set; }
    public Guid DeploymentId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public APIConfiguration Configuration { get; set; } = new();
    public APIKey[] ActiveKeys { get; set; } = Array.Empty<APIKey>();

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class APIConfiguration
{
    public int? MaxConcurrentRequests { get; set; }
    public int? RequestTimeoutSeconds { get; set; }
    public double? RateLimit { get; set; } // Requests per second
    public bool RequireAuthentication { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
}

public class APIKey
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class APIStats
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageLatencyMs { get; set; }
    public double ErrorRate { get; set; }
    public DateTime? LastRequestAt { get; set; }
}

/// <summary>
/// Model Serving Service for AI Engineers.
/// Handles real-time and batch predictions.
/// </summary>
public interface IModelServingService
{
    Task<PredictionResult> PredictAsync(Guid deploymentId, Dictionary<string, object> input);
    Task<BatchPredictionResult> BatchPredictAsync(Guid deploymentId, object[][] inputs);

    Task<PredictionResult> PredictWithExplanationAsync(Guid deploymentId, Dictionary<string, object> input);
    Task<PredictionPerformance> GetPredictionPerformanceAsync(Guid deploymentId);
}

public class PredictionResult
{
    public object? Prediction { get; set; }
    public double? Confidence { get; set; }
    public Dictionary<string, object>? Explanation { get; set; }
    public long LatencyMs { get; set; }
    public DateTime PredictedAt { get; set; }
}

public class BatchPredictionResult
{
    public object[] Predictions { get; set; } = Array.Empty<object>();
    public double[] Confidences { get; set; } = Array.Empty<double>();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long TotalLatencyMs { get; set; }
}

public class PredictionPerformance
{
    public long TotalPredictions { get; set; }
    public double AverageLatencyMs { get; set; }
    public double AverageConfidence { get; set; }
    public DateTime? LastPredictionAt { get; set; }
}

/// <summary>
/// Monitoring Service for AI Engineers.
/// Tracks deployment health, performance, and alerts.
/// </summary>
public interface IMonitoringService
{
    Task<HealthStatus> GetDeploymentHealthAsync(Guid deploymentId);
    Task<PerformanceMetrics> GetDeploymentPerformanceAsync(Guid deploymentId);

    Task SubscribeToAlertsAsync(Guid deploymentId, Func<Alert, Task> callback);
    Task<Alert[]> GetDeploymentAlertsAsync(Guid deploymentId);
    Task<Alert[]> GetActiveAlertsAsync(Guid deploymentId);

    Task SetAlertThresholdAsync(Guid deploymentId, AlertThreshold threshold);
    Task AcknowledgeAlertAsync(Guid alertId);
}

public class HealthStatus
{
    public Guid DeploymentId { get; set; }
    public HealthState State { get; set; }
    public double HealthScore { get; set; } // 0-100
    public string[] Issues { get; set; } = Array.Empty<string>();
    public DateTime CheckedAt { get; set; }
}

public enum HealthState
{
    Healthy,
    Degraded,
    Unhealthy
}

public class PerformanceMetrics
{
    public Guid DeploymentId { get; set; }
    public long RequestCount { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double ErrorRate { get; set; }
    public double? ModelAccuracy { get; set; }
    public DateTime MeasuredAt { get; set; }
}

public class Alert
{
    public Guid Id { get; set; }
    public Guid DeploymentId { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public class AlertThreshold
{
    public double? ErrorRateThreshold { get; set; } = 0.05;
    public double? LatencyThresholdMs { get; set; } = 1000;
    public double? ModelDriftThreshold { get; set; } = 0.1;
    public double? HealthScoreThreshold { get; set; } = 50;
}

/// <summary>
/// Drift Detection Service for AI Engineers.
/// Monitors data drift and model performance degradation.
/// </summary>
public interface IDriftDetectionService
{
    Task<DriftReport> AnalyzeDriftAsync(Guid deploymentId);
    Task<PerformanceDegradation> AnalyzePerformanceDegradationAsync(Guid deploymentId);

    Task SubscribeToDriftAlertsAsync(Guid deploymentId, Func<DriftAlert, Task> callback);
    Task<RetrainingRecommendation> GetRetrainingRecommendationAsync(Guid deploymentId);
}

public class DriftReport
{
    public Guid DeploymentId { get; set; }
    public double DriftScore { get; set; } // 0-1
    public DriftType Type { get; set; }
    public string[] AffectedFeatures { get; set; } = Array.Empty<string>();
    public string? Recommendation { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public enum DriftType
{
    Covariate,     // Feature distribution changes
    Label,         // Output/target changes
    Concept,       // Relationship between features and target changes
    None           // No significant drift
}

public class PerformanceDegradation
{
    public Guid DeploymentId { get; set; }
    public double DegradationPercentage { get; set; }
    public Dictionary<string, double> MetricChanges { get; set; } = new();
    public DateTime? LastRetrainingAt { get; set; }
    public bool RequiresRetraining { get; set; }
}

public class DriftAlert
{
    public Guid DeploymentId { get; set; }
    public DriftType Type { get; set; }
    public double DriftScore { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class RetrainingRecommendation
{
    public Guid DeploymentId { get; set; }
    public bool IsRecommended { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? ScheduledRetrainingAt { get; set; }
    public string[] RecommendedActions { get; set; } = Array.Empty<string>();
}

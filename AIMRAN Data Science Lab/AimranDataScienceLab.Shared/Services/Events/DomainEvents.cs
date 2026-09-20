using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIMRAN_Data_Science_Lab.Services.Events;

/// <summary>
/// Event Bus for inter-role communication and real-time synchronization.
/// Implements pub-sub pattern for cross-role notifications.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;
    Task SubscribeAsync<TEvent>(Guid aggregateId, Func<TEvent, Task> handler) where TEvent : DomainEvent;
}

/// <summary>
/// Base domain event for all cross-role events.
/// </summary>
public abstract class DomainEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string InitiatedByRole { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// ============================================================================
// DATASET EVENTS (initiated by Data Engineers and Analysts)
// ============================================================================

public class DatasetCreatedEvent : DomainEvent
{
    public Guid DatasetId { get; set; }
    public string DatasetName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string[] Columns { get; set; } = Array.Empty<string>();
}

public class DatasetCleanedEvent : DomainEvent
{
    public Guid DatasetId { get; set; }
    public Guid CleaningOperationId { get; set; }
    public int RowsRemoved { get; set; }
    public int MissingValuesHandled { get; set; }
    public Guid OutputDatasetId { get; set; }
}

public class DatasetVersionedEvent : DomainEvent
{
    public Guid DatasetId { get; set; }
    public int VersionNumber { get; set; }
    public string ChangeDescription { get; set; } = string.Empty;
}

public class DataQualityAssessmentEvent : DomainEvent
{
    public Guid DatasetId { get; set; }
    public double QualityScore { get; set; }
    public string[] IssuesFound { get; set; } = Array.Empty<string>();
}

public class DataAnomalyDetectedEvent : DomainEvent
{
    public Guid DatasetId { get; set; }
    public int AnomalyCount { get; set; }
    public string[] AnomalousColumns { get; set; } = Array.Empty<string>();
    public SeverityLevel Severity { get; set; }
}

// ============================================================================
// FEATURE EVENTS (initiated by Data Scientists)
// ============================================================================

public class FeatureCreatedEvent : DomainEvent
{
    public Guid FeatureSetId { get; set; }
    public Guid FeatureId { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public string FeatureType { get; set; } = string.Empty;
}

public class FeatureEngineeringCompletedEvent : DomainEvent
{
    public Guid FeatureSetId { get; set; }
    public int FeatureCount { get; set; }
    public string[] FeatureNames { get; set; } = Array.Empty<string>();
}

public class FeatureSelectionCompletedEvent : DomainEvent
{
    public Guid FeatureSetId { get; set; }
    public int SelectedFeatureCount { get; set; }
    public double Improvement { get; set; }
}

// ============================================================================
// MODEL EVENTS (initiated by Data Scientists and AI Engineers)
// ============================================================================

public class ModelCreatedEvent : DomainEvent
{
    public Guid ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public Guid TrainingDatasetId { get; set; }
}

public class ModelTrainingStartedEvent : DomainEvent
{
    public Guid ModelVersionId { get; set; }
    public Guid ModelId { get; set; }
    public Dictionary<string, object> Hyperparameters { get; set; } = new();
}

public class ModelTrainingCompletedEvent : DomainEvent
{
    public Guid ModelVersionId { get; set; }
    public Guid ModelId { get; set; }
    public int VersionNumber { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public long TrainingTimeMs { get; set; }
}

public class ModelEvaluatedEvent : DomainEvent
{
    public Guid ModelVersionId { get; set; }
    public Guid ModelId { get; set; }
    public Dictionary<string, double> EvaluationMetrics { get; set; } = new();
    public string[] Insights { get; set; } = Array.Empty<string>();
}

public class ModelApprovedEvent : DomainEvent
{
    public Guid ModelVersionId { get; set; }
    public Guid ModelId { get; set; }
    public string ApprovalReason { get; set; } = string.Empty;
}

public class ModelRejectedEvent : DomainEvent
{
    public Guid ModelVersionId { get; set; }
    public Guid ModelId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
}

public class ExperimentCompletedEvent : DomainEvent
{
    public Guid ExperimentId { get; set; }
    public Guid ModelId { get; set; }
    public Dictionary<string, double> Results { get; set; } = new();
    public bool IsBestResult { get; set; }
}

// ============================================================================
// PIPELINE EVENTS (initiated by Data Engineers)
// ============================================================================

public class PipelineCreatedEvent : DomainEvent
{
    public Guid PipelineId { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public string[] StepNames { get; set; } = Array.Empty<string>();
}

public class PipelineExecutionStartedEvent : DomainEvent
{
    public Guid ExecutionId { get; set; }
    public Guid PipelineId { get; set; }
    public string ExecutionTrigger { get; set; } = string.Empty; // "Manual", "Scheduled"
}

public class PipelineExecutionCompletedEvent : DomainEvent
{
    public Guid ExecutionId { get; set; }
    public Guid PipelineId { get; set; }
    public int StepsCompleted { get; set; }
    public int TotalSteps { get; set; }
    public long ExecutionTimeMs { get; set; }
}

public class PipelineExecutionFailedEvent : DomainEvent
{
    public Guid ExecutionId { get; set; }
    public Guid PipelineId { get; set; }
    public string FailingStep { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DataIngestedEvent : DomainEvent
{
    public Guid PipelineId { get; set; }
    public Guid OutputDatasetId { get; set; }
    public int RowsIngested { get; set; }
    public long BytesIngested { get; set; }
}

// ============================================================================
// DEPLOYMENT EVENTS (initiated by AI Engineers)
// ============================================================================

public class ModelDeploymentRequestedEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public Guid ModelVersionId { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
}

public class ModelDeploymentApprovedEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public Guid ModelVersionId { get; set; }
    public string ApprovedByRole { get; set; } = string.Empty;
}

public class ModelDeployedEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public Guid ModelVersionId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
}

public class ModelDeploymentFailedEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public Guid ModelVersionId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ModelRolledBackEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public Guid CurrentVersionId { get; set; }
    public Guid PreviousVersionId { get; set; }
    public string RollbackReason { get; set; } = string.Empty;
}

public class PredictionMadeEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public long LatencyMs { get; set; }
    public double? Confidence { get; set; }
}

public class ModelHealthAlertEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public AlertSeverity Severity { get; set; }
    public string AlertTitle { get; set; } = string.Empty;
    public string AlertDescription { get; set; } = string.Empty;
}

public class DataDriftDetectedEvent : DomainEvent
{
    public Guid DeploymentId { get; set; }
    public double DriftScore { get; set; }
    public string[] DriftingFeatures { get; set; } = Array.Empty<string>();
    public bool RetrainingRecommended { get; set; }
}

// ============================================================================
// INSIGHT EVENTS (initiated by AI Assistant and Analysts)
// ============================================================================

public class InsightGeneratedEvent : DomainEvent
{
    public Guid InsightId { get; set; }
    public Guid DatasetId { get; set; }
    public string InsightTitle { get; set; } = string.Empty;
    public string InsightDescription { get; set; } = string.Empty;
    public double Relevance { get; set; }
}

public class InsightApprovedEvent : DomainEvent
{
    public Guid InsightId { get; set; }
    public Guid ApprovedByUserId { get; set; }
}

public class ReportGeneratedEvent : DomainEvent
{
    public Guid ReportId { get; set; }
    public string ReportTitle { get; set; } = string.Empty;
    public Guid[] IncludedInsightIds { get; set; } = Array.Empty<Guid>();
}

// ============================================================================
// WORKFLOW EVENTS (cross-role coordination)
// ============================================================================

public class WorkflowPhaseTransitionEvent : DomainEvent
{
    new public Guid ProjectId { get; set; }
    public string FromPhase { get; set; } = string.Empty;
    public string ToPhase { get; set; } = string.Empty;
}

public class WorkflowStageCompletedEvent : DomainEvent
{
    public Guid WorkflowId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string AssignedRole { get; set; } = string.Empty;
}

public class WorkflowBlockedEvent : DomainEvent
{
    public Guid WorkflowId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RequiredAction { get; set; } = string.Empty;
}

// ============================================================================
// SEVERITY LEVEL FOR ALERTS
// ============================================================================

public enum SeverityLevel
{
    Info,
    Warning,
    Critical
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

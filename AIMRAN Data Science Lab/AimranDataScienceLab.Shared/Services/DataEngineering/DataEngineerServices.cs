using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AimranDataScienceLab.Core.Domain.DataPipelines;

namespace AimranDataScienceLab.Core.Services.DataEngineering;

/// <summary>
/// ETL Validation Result for data quality checks.
/// </summary>
public class ETLValidationResult
{
    public bool IsValid { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Pipeline Builder Service for Data Engineers.
/// Enables visual/programmatic pipeline creation and orchestration.
/// </summary>
public interface IPipelineBuilderService
{
    Task<DataPipeline> CreatePipelineAsync(DataPipeline pipeline);
    Task<DataPipeline?> GetPipelineAsync(Guid pipelineId);
    Task<DataPipeline[]> GetProjectPipelinesAsync(Guid projectId);
    Task UpdatePipelineAsync(DataPipeline pipeline);
    Task DeletePipelineAsync(Guid pipelineId);

    Task<PipelineStep> AddStepAsync(Guid pipelineId, PipelineStep step);
    Task UpdateStepAsync(Guid pipelineId, PipelineStep step);
    Task RemoveStepAsync(Guid pipelineId, Guid stepId);

    Task<PipelineExecution> ExecutePipelineAsync(Guid pipelineId);
    Task CancelExecutionAsync(Guid executionId);
    Task<PipelineExecution?> GetExecutionAsync(Guid executionId);
    Task<PipelineExecution[]> GetPipelineExecutionsAsync(Guid pipelineId);
}

/// <summary>
/// Data Connector Service for Data Engineers.
/// Manages connections to external data sources.
/// </summary>
public interface IDataConnectorService
{
    Task<DataConnector> CreateConnectorAsync(DataConnector connector);
    Task<DataConnector?> GetConnectorAsync(Guid connectorId);
    Task<DataConnector[]> GetProjectConnectorsAsync(Guid projectId);
    Task UpdateConnectorAsync(DataConnector connector);
    Task DeleteConnectorAsync(Guid connectorId);

    Task<bool> TestConnectionAsync(Guid connectorId);
    Task<string[]> GetAvailableTablesAsync(Guid connectorId);
    Task<string[]> GetAvailableSchemasAsync(Guid connectorId);

    Task<DataSourceMetadata> GetSourceMetadataAsync(Guid connectorId, string tableName);
}

public class DataSourceMetadata
{
    public string TableName { get; set; } = string.Empty;
    public string[] ColumnNames { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> ColumnTypes { get; set; } = new();
    public int EstimatedRowCount { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}

/// <summary>
/// ETL Service for Data Engineers.
/// Handles Extract, Transform, Load operations.
/// </summary>
public interface IETLService
{
    Task<ExtractResult> ExtractDataAsync(ExtractRequest request);
    Task<TransformResult> TransformDataAsync(TransformRequest request);
    Task<LoadResult> LoadDataAsync(LoadRequest request);

    Task<ETLValidationResult> ValidateDataAsync(Guid datasetId, ValidationRule[] rules);
    Task<ETLValidationResult> CheckDataQualityAsync(Guid datasetId);
}

public class ExtractRequest
{
    public Guid ConnectorId { get; set; }
    public string Query { get; set; } = string.Empty;
    public int? RowLimit { get; set; }
    public DateTime? IncrementalFrom { get; set; }
}

public class ExtractResult
{
    public int RowsExtracted { get; set; }
    public long BytesExtracted { get; set; }
    public long ExtractTimeMs { get; set; }
    public string[]? ErrorMessages { get; set; }
}

public class TransformRequest
{
    public Guid SourceDatasetId { get; set; }
    public TransformOperation[] Operations { get; set; } = Array.Empty<TransformOperation>();
}

public class TransformOperation
{
    public string Type { get; set; } = string.Empty; // Filter, Aggregate, Join, etc.
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class TransformResult
{
    public Guid OutputDatasetId { get; set; }
    public int RowsTransformed { get; set; }
    public long TransformTimeMs { get; set; }
}

public class LoadRequest
{
    public Guid SourceDatasetId { get; set; }
    public Guid TargetConnectorId { get; set; }
    public string TargetTable { get; set; } = string.Empty;
    public LoadMode Mode { get; set; } = LoadMode.Insert;
}

public enum LoadMode
{
    Insert,
    Update,
    Upsert,
    Replace
}

public class LoadResult
{
    public int RowsLoaded { get; set; }
    public int RowsFailed { get; set; }
    public long LoadTimeMs { get; set; }
}

/// <summary>
/// Data Quality Service for Data Engineers.
/// Monitors and validates data quality.
/// </summary>
public interface IDataQualityService
{
    Task<DataQualityReport> AssessQualityAsync(Guid datasetId);
    Task<DataQualityReport[]> GetQualityHistoryAsync(Guid datasetId);
    Task<DataAnomalyReport> DetectAnomaliesAsync(Guid datasetId);
    Task ScheduleQualityCheckAsync(Guid pipelineId, QualityCheckSchedule schedule);
}

public class DataQualityReport
{
    public Guid DatasetId { get; set; }
    public double OverallScore { get; set; }
    public int IssuesFound { get; set; }
    public int WarningsFound { get; set; }
    public DataQualityIssue[] Issues { get; set; } = Array.Empty<DataQualityIssue>();
    public DateTime AssessedAt { get; set; }
}

public class DataQualityIssue
{
    public string Category { get; set; } = string.Empty; // Missing, Outlier, Invalid, etc.
    public string ColumnName { get; set; } = string.Empty;
    public int AffectedRows { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public SeverityLevel Severity { get; set; }
}

public enum SeverityLevel
{
    Info,
    Warning,
    Error,
    Critical
}

public class DataAnomalyReport
{
    public Guid DatasetId { get; set; }
    public int AnomalyCount { get; set; }
    public string[] AnomalousColumns { get; set; } = Array.Empty<string>();
    public Dictionary<string, int[]> AnomalousRowIndices { get; set; } = new();
}

public class QualityCheckSchedule
{
    public string CronExpression { get; set; } = string.Empty;
    public ValidationRule[] Rules { get; set; } = Array.Empty<ValidationRule>();
    public string[] NotificationEmails { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Scheduler Service for Data Engineers.
/// Manages pipeline scheduling and triggers.
/// </summary>
public interface ISchedulerService
{
    Task SchedulePipelineAsync(Guid pipelineId, PipelineSchedule schedule);
    Task<ScheduledPipeline?> GetScheduleAsync(Guid pipelineId);
    Task UpdateScheduleAsync(Guid pipelineId, PipelineSchedule schedule);
    Task CancelScheduleAsync(Guid pipelineId);

    Task<ScheduledExecution[]> GetUpcomingExecutionsAsync(Guid pipelineId);
    Task TriggerPipelineAsync(Guid pipelineId);
}

public class ScheduledPipeline
{
    public Guid PipelineId { get; set; }
    public PipelineSchedule Schedule { get; set; } = new();
    public bool IsEnabled { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? NextExecutionAt { get; set; }
}

public class ScheduledExecution
{
    public DateTime ScheduledTime { get; set; }
    public ExecutionStatus Status { get; set; }
    public DateTime? ActualExecutionTime { get; set; }
}

/// <summary>
/// Data validation rule.
/// </summary>
public class ValidationRule
{
    public string ColumnName { get; set; } = string.Empty;
    public RuleType Type { get; set; }
    public object? Value { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum RuleType
{
    NotNull,
    Unique,
    Range,
    Pattern,
    Length,
    CustomExpression
}

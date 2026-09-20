using System;
using System.Collections.Generic;

namespace AimranDataScienceLab.Core.Domain.DataPipelines;

/// <summary>
/// Represents a data pipeline for Data Engineers.
/// Can include data ingestion, transformation, cleaning, and enrichment.
/// </summary>
public class DataPipeline
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public PipelineType Type { get; set; }
    public PipelineStatus Status { get; set; } = PipelineStatus.Draft;

    public PipelineStep[] Steps { get; set; } = Array.Empty<PipelineStep>();
    public PipelineSchedule? Schedule { get; set; }

    public Guid? SourceDatasetId { get; set; }
    public Guid? TargetDatasetId { get; set; }

    public PipelineMetrics Metrics { get; set; } = new();

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastExecutedAt { get; set; }
}

public enum PipelineType
{
    DataIngestion,
    DataTransformation,
    DataCleaning,
    FeatureEngineering,
    DataQualityCheck,
    Custom
}

public enum PipelineStatus
{
    Draft,
    Active,
    Paused,
    Failed,
    Archived
}

public class PipelineStep
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public PipelineStepType Type { get; set; }

    public Dictionary<string, object> Configuration { get; set; } = new();
    public string[]? InputDatasets { get; set; }
    public string? OutputDataset { get; set; }

    public TimeSpan? EstimatedDuration { get; set; }
    public int? RetryAttempts { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}

public enum PipelineStepType
{
    Read,
    Extract,
    Transform,
    Validate,
    Clean,
    Enrich,
    Aggregate,
    Write,
    Execute,
    Custom
}

public class PipelineSchedule
{
    public ScheduleType Type { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public DateTime? NextExecutionAt { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public enum ScheduleType
{
    Manual,
    Once,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Custom
}

public class PipelineMetrics
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double AverageDurationSeconds { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public string? LastError { get; set; }
    public int DataProcessedRows { get; set; }
    public long DataProcessedBytes { get; set; }
}

/// <summary>
/// Pipeline execution record for audit and debugging.
/// </summary>
public class PipelineExecution
{
    public Guid Id { get; set; }
    public Guid PipelineId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;

    public int StepsCompleted { get; set; }
    public int StepsFailed { get; set; }

    public string Logs { get; set; } = string.Empty;
    public Dictionary<string, object> Result { get; set; } = new();

    public Guid TriggeredByUserId { get; set; }
}

public enum ExecutionStatus
{
    Running,
    Completed,
    Failed,
    Cancelled,
    Paused
}

/// <summary>
/// Data connector for external data sources.
/// </summary>
public class DataConnector
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ConnectorType Type { get; set; }
    public Dictionary<string, object> ConnectionParams { get; set; } = new();
    public bool TestConnection { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
}

public enum ConnectorType
{
    SqlServer,
    PostgreSQL,
    MySQL,
    MongoDB,
    Elasticsearch,
    API,
    S3,
    AzureBlob,
    SFTP,
    Salesforce,
    Shopify,
    Custom
}

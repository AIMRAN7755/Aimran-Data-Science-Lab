using System;
using System.Collections.Generic;

namespace AimranDataScienceLab.Core.Domain.DataManagement;

/// <summary>
/// Represents a dataset with full lineage tracking, versioning, and role-based access.
/// </summary>
public class Dataset
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DatasetSource Source { get; set; } = new();
    public DatasetStatistics Statistics { get; set; } = new();
    public DatasetQuality Quality { get; set; } = new();

    public Guid[] VersionIds { get; set; } = Array.Empty<Guid>(); // Dataset version history
    public Guid CurrentVersionId { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Lineage tracking
    public Guid? SourceDatasetId { get; set; } // If derived from another dataset
    public string? TransformationApplied { get; set; } // JSON of transformations
}

public class DatasetSource
{
    public DataSourceType Type { get; set; }
    public string Location { get; set; } = string.Empty;
    public Dictionary<string, object> ConnectionParams { get; set; } = new();
    public DateTime? LastRefreshAt { get; set; }
}

public enum DataSourceType
{
    Upload,
    SqlServer,
    PostgreSQL,
    MongoDB,
    API,
    ExcelFile,
    CSV,
    Parquet,
    StreamingSource
}

public class DatasetStatistics
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public long SizeBytes { get; set; }
    public DateTime? ComputedAt { get; set; }

    public ColumnStatistic[] ColumnStats { get; set; } = Array.Empty<ColumnStatistic>();
}

public class ColumnStatistic
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int MissingCount { get; set; }
    public double MissingPercent { get; set; }
    public double? Mean { get; set; }
    public double? Median { get; set; }
    public double? StdDev { get; set; }
    public int? UniqueCount { get; set; }
}

public class DatasetQuality
{
    public double OverallScore { get; set; } // 0-100
    public int MissingValuesCount { get; set; }
    public int DuplicateRowsCount { get; set; }
    public int AnomaliesDetected { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    public DateTime? AssessedAt { get; set; }
}

/// <summary>
/// Tracks dataset versions for reproducibility and rollback.
/// </summary>
public class DatasetVersion
{
    public Guid Id { get; set; }
    public Guid DatasetId { get; set; }
    public int VersionNumber { get; set; }
    public string Description { get; set; } = string.Empty;

    public string ChangeLog { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DatasetStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Cleaning operation applied to dataset.
/// Used by Data Analysts and Engineers for data preparation.
/// </summary>
public class DataCleaningOperation
{
    public Guid Id { get; set; }
    public Guid DatasetId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;
    public CleaningOperationType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();

    public bool IsApplied { get; set; }
    public DateTime? AppliedAt { get; set; }
    public Guid? ResultDatasetId { get; set; } // Output dataset after cleaning

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum CleaningOperationType
{
    RemoveNull,
    FillNull,
    RemoveDuplicates,
    RemoveOutliers,
    TypeConversion,
    NormalizationScaling,
    Encoding,
    FeatureExtraction,
    Custom
}

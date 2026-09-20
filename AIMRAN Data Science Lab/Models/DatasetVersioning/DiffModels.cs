namespace AIMRAN_Data_Science_Lab.Models.DatasetVersioning;

/// <summary>
/// Result of comparing two dataset versions.
/// </summary>
public record DatasetDiffResult
{
    public DatasetVersionId FromVersionId { get; init; }
    public DatasetVersionId ToVersionId { get; init; }
    public int FromVersionNumber { get; init; }
    public int ToVersionNumber { get; init; }
    public DateTime ComparedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan ComputeDuration { get; init; }
    
    // Row-level changes
    public int RowsAdded { get; init; }
    public int RowsRemoved { get; init; }
    public int RowsModified { get; init; }
    public int RowsUnchanged { get; init; }
    public int TotalRowsBefore { get; init; }
    public int TotalRowsAfter { get; init; }
    
    // Cell-level changes
    public long CellsModified { get; init; }
    public double ChangePercentage { get; init; }
    
    // Schema changes
    public SchemaDiff SchemaDiff { get; init; } = new();
    
    // Column-level diffs
    public IReadOnlyList<ColumnDiff> ColumnDiffs { get; init; } = [];
    
    // Sample changes for preview
    public IReadOnlyList<RowChange> SampleAddedRows { get; init; } = [];
    public IReadOnlyList<RowChange> SampleRemovedRows { get; init; } = [];
    public IReadOnlyList<RowModification> SampleModifiedRows { get; init; } = [];
    
    // Statistics
    public DiffStatistics Statistics { get; init; } = new();
    
    // Summary
    public string Summary { get; init; } = string.Empty;
    public DiffSeverity Severity { get; init; } = DiffSeverity.Minor;
}

/// <summary>
/// Schema differences between versions.
/// </summary>
public record SchemaDiff
{
    public bool HasSchemaChanges => ColumnsAdded.Count > 0 || ColumnsRemoved.Count > 0 || ColumnsRenamed.Count > 0 || TypeChanges.Count > 0;
    public IReadOnlyList<ColumnSchema> ColumnsAdded { get; init; } = [];
    public IReadOnlyList<ColumnSchema> ColumnsRemoved { get; init; } = [];
    public IReadOnlyList<ColumnRename> ColumnsRenamed { get; init; } = [];
    public IReadOnlyList<ColumnTypeChange> TypeChanges { get; init; } = [];
    public IReadOnlyList<ColumnOrderChange> OrderChanges { get; init; } = [];
}

/// <summary>
/// Schema information for a column.
/// </summary>
public record ColumnSchema
{
    public string Name { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public int Index { get; init; }
    public bool IsNullable { get; init; }
}

/// <summary>
/// Column rename information.
/// </summary>
public record ColumnRename
{
    public string OldName { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;
    public double Confidence { get; init; }
}

/// <summary>
/// Column type change information.
/// </summary>
public record ColumnTypeChange
{
    public string ColumnName { get; init; } = string.Empty;
    public string OldType { get; init; } = string.Empty;
    public string NewType { get; init; } = string.Empty;
    public bool IsCompatible { get; init; }
}

/// <summary>
/// Column order change information.
/// </summary>
public record ColumnOrderChange
{
    public string ColumnName { get; init; } = string.Empty;
    public int OldIndex { get; init; }
    public int NewIndex { get; init; }
}

/// <summary>
/// Differences for a single column.
/// </summary>
public record ColumnDiff
{
    public string ColumnName { get; init; } = string.Empty;
    public ColumnDiffType DiffType { get; init; }
    public int ValuesAdded { get; init; }
    public int ValuesRemoved { get; init; }
    public int ValuesModified { get; init; }
    public double ChangePercentage { get; init; }
    
    // Statistics changes
    public ColumnStatsDiff? StatsDiff { get; init; }
    
    // Sample value changes
    public IReadOnlyList<ValueChange> SampleChanges { get; init; } = [];
}

/// <summary>
/// Statistical differences for a column.
/// </summary>
public record ColumnStatsDiff
{
    public double? MeanBefore { get; init; }
    public double? MeanAfter { get; init; }
    public double? StdDevBefore { get; init; }
    public double? StdDevAfter { get; init; }
    public double? MissingPercentBefore { get; init; }
    public double? MissingPercentAfter { get; init; }
    public int? UniqueCountBefore { get; init; }
    public int? UniqueCountAfter { get; init; }
}

/// <summary>
/// A single value change.
/// </summary>
public record ValueChange
{
    public int RowIndex { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public ValueChangeType ChangeType { get; init; }
}

/// <summary>
/// Represents a row that was added or removed.
/// </summary>
public record RowChange
{
    public int RowIndex { get; init; }
    public string RowKey { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string?> Values { get; init; } = new Dictionary<string, string?>();
}

/// <summary>
/// Represents a modified row with before/after values.
/// </summary>
public record RowModification
{
    public int RowIndex { get; init; }
    public string RowKey { get; init; } = string.Empty;
    public IReadOnlyList<ValueChange> Changes { get; init; } = [];
    public int ColumnsAffected { get; init; }
}

/// <summary>
/// Statistical summary of differences.
/// </summary>
public record DiffStatistics
{
    public double DataSimilarity { get; init; }
    public double SchemaSimilarity { get; init; }
    public double OverallSimilarity { get; init; }
    public IReadOnlyDictionary<string, double> ColumnSimilarities { get; init; } = new Dictionary<string, double>();
    public string MostChangedColumn { get; init; } = string.Empty;
    public double MostChangedColumnPercent { get; init; }
}

public enum ColumnDiffType
{
    Unchanged,
    Added,
    Removed,
    Modified,
    TypeChanged,
    Renamed
}

public enum ValueChangeType
{
    Added,
    Removed,
    Modified,
    TypeChanged,
    NullToValue,
    ValueToNull
}

public enum DiffSeverity
{
    None,
    Minor,
    Moderate,
    Major,
    Breaking
}

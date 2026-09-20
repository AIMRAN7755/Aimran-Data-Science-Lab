namespace AIMRAN_Data_Science_Lab.Models;

/// <summary>
/// Represents a dataset in the DS-Workbench.
/// </summary>
public record Dataset
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string FilePath { get; init; }
    public long SizeBytes { get; init; }
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public DatasetFormat Format { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
    public int Version { get; init; } = 1;
    public DatasetStorageLocation StorageLocation { get; init; } = DatasetStorageLocation.Local;
    public string? AzureBlobUrl { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public enum DatasetFormat
{
    Csv,
    Parquet,
    Json,
    Excel,
    Feather,
    Pickle
}

public enum DatasetStorageLocation
{
    Local,
    AzureBlob,
    Hybrid
}

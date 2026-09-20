using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local in-memory implementation of the dataset service.
/// In production, this would use SQLite or another persistent store.
/// </summary>
public sealed class LocalDatasetService : IDatasetService
{
    private readonly List<Dataset> _datasets = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<Dataset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Dataset>>(_datasets.ToList());
        }
    }

    public Task<Dataset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_datasets.FirstOrDefault(d => d.Id == id));
        }
    }

    public Task<Dataset> CreateAsync(string name, string filePath, string? description = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fileInfo = new FileInfo(filePath);
        var format = GetFormatFromExtension(fileInfo.Extension);

        var dataset = new Dataset
        {
            Name = name,
            Description = description,
            FilePath = filePath,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Format = format,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _datasets.Add(dataset);
        }

        return Task.FromResult(dataset);
    }

    public async Task<Dataset> ImportFromFileAsync(string sourceFilePath, string? name = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Source file not found.", sourceFilePath);
        }

        var fileName = name ?? Path.GetFileNameWithoutExtension(sourceFilePath);
        return await CreateAsync(fileName, sourceFilePath, cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _datasets.RemoveAll(d => d.Id == id);
        }
        return Task.CompletedTask;
    }

    public async Task<Dataset> CreateVersionAsync(Guid datasetId, string newFilePath, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(datasetId, cancellationToken) 
            ?? throw new InvalidOperationException($"Dataset {datasetId} not found.");

        var fileInfo = new FileInfo(newFilePath);
        var newDataset = existing with
        {
            Id = Guid.NewGuid(),
            FilePath = newFilePath,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Version = existing.Version + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _datasets.Add(newDataset);
        }

        return newDataset;
    }

    public Task<DatasetPreview> GetPreviewAsync(Guid id, int rowCount = 100, CancellationToken cancellationToken = default)
    {
        // Placeholder - real implementation would read file and parse data
        return Task.FromResult(new DatasetPreview
        {
            Columns = ["Column1", "Column2", "Column3"],
            Rows = [["Value1", "Value2", "Value3"]],
            TotalRows = 1
        });
    }

    private static DatasetFormat GetFormatFromExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".csv" => DatasetFormat.Csv,
        ".parquet" => DatasetFormat.Parquet,
        ".json" => DatasetFormat.Json,
        ".xlsx" or ".xls" => DatasetFormat.Excel,
        ".feather" => DatasetFormat.Feather,
        ".pkl" or ".pickle" => DatasetFormat.Pickle,
        _ => DatasetFormat.Csv
    };
}

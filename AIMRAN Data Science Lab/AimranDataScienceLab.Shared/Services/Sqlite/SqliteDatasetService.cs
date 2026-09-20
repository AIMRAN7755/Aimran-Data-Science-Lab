using System.Text.Json;
using AimranDataScienceLab.Engine.Data;
using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Sqlite;

/// <summary>
/// SQLite-backed implementation of the dataset service.
/// </summary>
public sealed class SqliteDatasetService : IDatasetService
{
    private readonly SqliteConnectionFactory _db;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SqliteDatasetService(SqliteConnectionFactory db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Dataset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM datasets ORDER BY created_at DESC;";

        var datasets = new List<Dataset>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            datasets.Add(MapDataset(reader));
        }

        return Task.FromResult<IReadOnlyList<Dataset>>(datasets);
    }

    public Task<Dataset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM datasets WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        using var reader = cmd.ExecuteReader();
        return Task.FromResult(reader.Read() ? MapDataset(reader) : null);
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

        InsertDataset(dataset);
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

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM datasets WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();

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

        InsertDataset(newDataset);
        return newDataset;
    }

    public Task<DatasetPreview> GetPreviewAsync(Guid id, int rowCount = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Real implementation will parse the actual file via the Rust engine
        return Task.FromResult(new DatasetPreview
        {
            Columns = ["Column1", "Column2", "Column3"],
            Rows = [["Value1", "Value2", "Value3"]],
            TotalRows = 1
        });
    }

    private void InsertDataset(Dataset dataset)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO datasets (id, name, description, file_path, size_bytes, row_count, column_count,
                                  format, created_at, updated_at, version, storage_location, azure_blob_url, tags)
            VALUES ($id, $name, $desc, $path, $size, $rows, $cols,
                    $fmt, $created, $updated, $ver, $loc, $blob, $tags);
            """;
        cmd.Parameters.AddWithValue("$id", dataset.Id.ToString());
        cmd.Parameters.AddWithValue("$name", dataset.Name);
        cmd.Parameters.AddWithValue("$desc", (object?)dataset.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$path", dataset.FilePath);
        cmd.Parameters.AddWithValue("$size", dataset.SizeBytes);
        cmd.Parameters.AddWithValue("$rows", dataset.RowCount);
        cmd.Parameters.AddWithValue("$cols", dataset.ColumnCount);
        cmd.Parameters.AddWithValue("$fmt", dataset.Format.ToString());
        cmd.Parameters.AddWithValue("$created", dataset.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", dataset.UpdatedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ver", dataset.Version);
        cmd.Parameters.AddWithValue("$loc", dataset.StorageLocation.ToString());
        cmd.Parameters.AddWithValue("$blob", (object?)dataset.AzureBlobUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(dataset.Tags, _jsonOptions));
        cmd.ExecuteNonQuery();
    }

    private Dataset MapDataset(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        var tagsJson = reader.IsDBNull(reader.GetOrdinal("tags")) ? "[]" : reader.GetString(reader.GetOrdinal("tags"));
        var tags = JsonSerializer.Deserialize<List<string>>(tagsJson, _jsonOptions) ?? [];

        return new Dataset
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            SizeBytes = reader.GetInt64(reader.GetOrdinal("size_bytes")),
            RowCount = reader.GetInt32(reader.GetOrdinal("row_count")),
            ColumnCount = reader.GetInt32(reader.GetOrdinal("column_count")),
            Format = Enum.Parse<DatasetFormat>(reader.GetString(reader.GetOrdinal("format"))),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at"))),
            Version = reader.GetInt32(reader.GetOrdinal("version")),
            StorageLocation = Enum.Parse<DatasetStorageLocation>(reader.GetString(reader.GetOrdinal("storage_location"))),
            AzureBlobUrl = reader.IsDBNull(reader.GetOrdinal("azure_blob_url")) ? null : reader.GetString(reader.GetOrdinal("azure_blob_url")),
            Tags = tags
        };
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

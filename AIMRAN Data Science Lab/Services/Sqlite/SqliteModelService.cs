using System.Text.Json;
using AimranDataScienceLab.Engine.Data;
using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Sqlite;

/// <summary>
/// SQLite-backed implementation of the model service.
/// </summary>
internal sealed class SqliteModelService : IModelService
{
    private readonly SqliteConnectionFactory _db;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SqliteModelService(SqliteConnectionFactory db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<MlModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM models ORDER BY created_at DESC;";

        var models = new List<MlModel>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            models.Add(MapModel(reader));
        }

        return Task.FromResult<IReadOnlyList<MlModel>>(models);
    }

    public Task<MlModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM models WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        using var reader = cmd.ExecuteReader();
        return Task.FromResult(reader.Read() ? MapModel(reader) : null);
    }

    public Task<MlModel> RegisterAsync(
        string name,
        Guid experimentId,
        string algorithm,
        string framework,
        string filePath,
        IDictionary<string, double>? metrics = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(framework);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fileInfo = new FileInfo(filePath);
        var metricsDict = metrics is not null
            ? new Dictionary<string, double>(metrics).AsReadOnly()
            : new Dictionary<string, double>().AsReadOnly();

        var model = new MlModel
        {
            Name = name,
            ExperimentId = experimentId,
            Algorithm = algorithm,
            Framework = framework,
            FilePath = filePath,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Status = ModelStatus.Registered,
            PerformanceMetrics = metricsDict,
            CreatedAt = DateTime.UtcNow
        };

        InsertModel(model);
        return Task.FromResult(model);
    }

    public async Task<MlModel> CreateVersionAsync(Guid modelId, string newFilePath, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(modelId, cancellationToken)
            ?? throw new InvalidOperationException($"Model {modelId} not found.");

        var fileInfo = new FileInfo(newFilePath);
        var newModel = existing with
        {
            Id = Guid.NewGuid(),
            FilePath = newFilePath,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            Version = existing.Version + 1,
            Status = ModelStatus.Registered,
            CreatedAt = DateTime.UtcNow
        };

        InsertModel(newModel);
        return newModel;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM models WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<ModelTestResult> TestAsync(Guid modelId, Guid testDatasetId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Delegated to Python engine in hybrid mode
        return Task.FromResult(new ModelTestResult
        {
            ModelId = modelId,
            DatasetId = testDatasetId,
            Metrics = new Dictionary<string, double> { ["accuracy"] = 0.0 }.AsReadOnly(),
            TestedAt = DateTime.UtcNow
        });
    }

    public async Task<MlModel> DeployAsync(Guid modelId, string endpoint, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(modelId, cancellationToken)
            ?? throw new InvalidOperationException($"Model {modelId} not found.");

        var updated = existing with
        {
            Status = ModelStatus.Deployed,
            IsDeployed = true,
            DeploymentEndpoint = endpoint
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE models SET status = $status, is_deployed = 1, deployment_endpoint = $ep WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", modelId.ToString());
        cmd.Parameters.AddWithValue("$status", updated.Status.ToString());
        cmd.Parameters.AddWithValue("$ep", endpoint);
        cmd.ExecuteNonQuery();

        return updated;
    }

    private void InsertModel(MlModel model)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO models (id, name, description, experiment_id, algorithm, framework, version,
                                file_path, size_bytes, created_at, status, performance_metrics,
                                hyperparameters, azure_ml_model_id, is_deployed, deployment_endpoint)
            VALUES ($id, $name, $desc, $eid, $algo, $fw, $ver,
                    $path, $size, $created, $status, $metrics,
                    $params, $azure, $deployed, $endpoint);
            """;
        cmd.Parameters.AddWithValue("$id", model.Id.ToString());
        cmd.Parameters.AddWithValue("$name", model.Name);
        cmd.Parameters.AddWithValue("$desc", (object?)model.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$eid", model.ExperimentId.ToString());
        cmd.Parameters.AddWithValue("$algo", model.Algorithm);
        cmd.Parameters.AddWithValue("$fw", model.Framework);
        cmd.Parameters.AddWithValue("$ver", model.Version);
        cmd.Parameters.AddWithValue("$path", model.FilePath);
        cmd.Parameters.AddWithValue("$size", model.SizeBytes);
        cmd.Parameters.AddWithValue("$created", model.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$status", model.Status.ToString());
        cmd.Parameters.AddWithValue("$metrics", JsonSerializer.Serialize(model.PerformanceMetrics, _jsonOptions));
        cmd.Parameters.AddWithValue("$params", JsonSerializer.Serialize(model.Hyperparameters, _jsonOptions));
        cmd.Parameters.AddWithValue("$azure", (object?)model.AzureMlModelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$deployed", model.IsDeployed ? 1 : 0);
        cmd.Parameters.AddWithValue("$endpoint", (object?)model.DeploymentEndpoint ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private MlModel MapModel(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        var metricsJson = reader.IsDBNull(reader.GetOrdinal("performance_metrics")) ? "{}" : reader.GetString(reader.GetOrdinal("performance_metrics"));
        var paramsJson = reader.IsDBNull(reader.GetOrdinal("hyperparameters")) ? "{}" : reader.GetString(reader.GetOrdinal("hyperparameters"));

        return new MlModel
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            ExperimentId = Guid.Parse(reader.GetString(reader.GetOrdinal("experiment_id"))),
            Algorithm = reader.GetString(reader.GetOrdinal("algorithm")),
            Framework = reader.GetString(reader.GetOrdinal("framework")),
            Version = reader.GetInt32(reader.GetOrdinal("version")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            SizeBytes = reader.GetInt64(reader.GetOrdinal("size_bytes")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            Status = Enum.Parse<ModelStatus>(reader.GetString(reader.GetOrdinal("status"))),
            PerformanceMetrics = JsonSerializer.Deserialize<Dictionary<string, double>>(metricsJson, _jsonOptions)?.AsReadOnly() ?? new Dictionary<string, double>().AsReadOnly(),
            Hyperparameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson, _jsonOptions)?.AsReadOnly() ?? new Dictionary<string, object>().AsReadOnly(),
            AzureMlModelId = reader.IsDBNull(reader.GetOrdinal("azure_ml_model_id")) ? null : reader.GetString(reader.GetOrdinal("azure_ml_model_id")),
            IsDeployed = reader.GetInt32(reader.GetOrdinal("is_deployed")) == 1,
            DeploymentEndpoint = reader.IsDBNull(reader.GetOrdinal("deployment_endpoint")) ? null : reader.GetString(reader.GetOrdinal("deployment_endpoint"))
        };
    }
}

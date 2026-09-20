using System.Runtime.CompilerServices;
using System.Text.Json;
using AimranDataScienceLab.Engine.Data;
using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Sqlite;

/// <summary>
/// SQLite-backed implementation of the experiment service.
/// </summary>
internal sealed class SqliteExperimentService : IExperimentService
{
    private readonly SqliteConnectionFactory _db;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SqliteExperimentService(SqliteConnectionFactory db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Experiment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM experiments ORDER BY created_at DESC;";

        var experiments = new List<Experiment>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var exp = MapExperiment(reader);
            experiments.Add(exp with { Runs = LoadRuns(exp.Id) });
        }

        return Task.FromResult<IReadOnlyList<Experiment>>(experiments);
    }

    public Task<IReadOnlyList<Experiment>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM experiments WHERE project_id = $pid ORDER BY created_at DESC;";
        cmd.Parameters.AddWithValue("$pid", projectId.ToString());

        var experiments = new List<Experiment>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var exp = MapExperiment(reader);
            experiments.Add(exp with { Runs = LoadRuns(exp.Id) });
        }

        return Task.FromResult<IReadOnlyList<Experiment>>(experiments);
    }

    public Task<Experiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM experiments WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return Task.FromResult<Experiment?>(null);

        var exp = MapExperiment(reader);
        return Task.FromResult<Experiment?>(exp with { Runs = LoadRuns(exp.Id) });
    }

    public Task<Experiment> CreateAsync(string name, Guid projectId, Guid datasetId, string? description = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        var experiment = new Experiment
        {
            Name = name,
            Description = description,
            ProjectId = projectId,
            DatasetId = datasetId,
            Status = ExperimentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO experiments (id, name, description, project_id, dataset_id, status, created_at, compute_target, hyperparameters, metrics)
            VALUES ($id, $name, $desc, $pid, $did, $status, $created, $target, $params, $metrics);
            """;
        cmd.Parameters.AddWithValue("$id", experiment.Id.ToString());
        cmd.Parameters.AddWithValue("$name", experiment.Name);
        cmd.Parameters.AddWithValue("$desc", (object?)experiment.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pid", experiment.ProjectId.ToString());
        cmd.Parameters.AddWithValue("$did", experiment.DatasetId.ToString());
        cmd.Parameters.AddWithValue("$status", experiment.Status.ToString());
        cmd.Parameters.AddWithValue("$created", experiment.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$target", experiment.ComputeTarget.ToString());
        cmd.Parameters.AddWithValue("$params", JsonSerializer.Serialize(experiment.Hyperparameters, _jsonOptions));
        cmd.Parameters.AddWithValue("$metrics", JsonSerializer.Serialize(experiment.Metrics, _jsonOptions));
        cmd.ExecuteNonQuery();

        return Task.FromResult(experiment);
    }

    public Task<Experiment> StartAsync(Guid experimentId, IDictionary<string, object>? hyperparameters = null, ComputeTarget target = ComputeTarget.Local, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = GetByIdAsync(experimentId, cancellationToken).Result
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found.");

        var hyperparamsDict = hyperparameters is not null
            ? new Dictionary<string, object>(hyperparameters).AsReadOnly()
            : new Dictionary<string, object>().AsReadOnly();

        var updated = existing with
        {
            Status = ExperimentStatus.Running,
            StartedAt = DateTime.UtcNow,
            ComputeTarget = target,
            Hyperparameters = hyperparamsDict
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE experiments SET status = $status, started_at = $started, compute_target = $target, hyperparameters = $params
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", experimentId.ToString());
        cmd.Parameters.AddWithValue("$status", updated.Status.ToString());
        cmd.Parameters.AddWithValue("$started", updated.StartedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$target", updated.ComputeTarget.ToString());
        cmd.Parameters.AddWithValue("$params", JsonSerializer.Serialize(updated.Hyperparameters, _jsonOptions));
        cmd.ExecuteNonQuery();

        return Task.FromResult(updated);
    }

    public Task<Experiment> StopAsync(Guid experimentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = GetByIdAsync(experimentId, cancellationToken).Result
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found.");

        var updated = existing with
        {
            Status = ExperimentStatus.Cancelled,
            CompletedAt = DateTime.UtcNow
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE experiments SET status = $status, completed_at = $completed WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", experimentId.ToString());
        cmd.Parameters.AddWithValue("$status", updated.Status.ToString());
        cmd.Parameters.AddWithValue("$completed", updated.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        return Task.FromResult(updated);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM experiments WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<Experiment> LogMetricsAsync(Guid experimentId, IDictionary<string, double> metrics, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = GetByIdAsync(experimentId, cancellationToken).Result
            ?? throw new InvalidOperationException($"Experiment {experimentId} not found.");

        var merged = new Dictionary<string, double>(existing.Metrics);
        foreach (var kvp in metrics)
        {
            merged[kvp.Key] = kvp.Value;
        }

        var updated = existing with { Metrics = merged.AsReadOnly() };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE experiments SET metrics = $metrics WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", experimentId.ToString());
        cmd.Parameters.AddWithValue("$metrics", JsonSerializer.Serialize(updated.Metrics, _jsonOptions));
        cmd.ExecuteNonQuery();

        return Task.FromResult(updated);
    }

    public async IAsyncEnumerable<ExperimentRun> StreamRunsAsync(
        Guid experimentId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var runs = LoadRuns(experimentId);
        foreach (var run in runs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return run;
            await Task.Delay(10, cancellationToken);
        }
    }

    private IReadOnlyList<ExperimentRun> LoadRuns(Guid experimentId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM experiment_runs WHERE experiment_id = $eid ORDER BY run_number;";
        cmd.Parameters.AddWithValue("$eid", experimentId.ToString());

        var runs = new List<ExperimentRun>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var metricsJson = reader.IsDBNull(reader.GetOrdinal("metrics")) ? "{}" : reader.GetString(reader.GetOrdinal("metrics"));
            runs.Add(new ExperimentRun
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
                RunNumber = reader.GetInt32(reader.GetOrdinal("run_number")),
                StartedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("started_at"))),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("completed_at"))),
                Status = Enum.Parse<ExperimentStatus>(reader.GetString(reader.GetOrdinal("status"))),
                Metrics = JsonSerializer.Deserialize<Dictionary<string, double>>(metricsJson, _jsonOptions)?.AsReadOnly() ?? new Dictionary<string, double>().AsReadOnly(),
                LogOutput = reader.IsDBNull(reader.GetOrdinal("log_output")) ? null : reader.GetString(reader.GetOrdinal("log_output")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("error_message")) ? null : reader.GetString(reader.GetOrdinal("error_message"))
            });
        }

        return runs;
    }

    private Experiment MapExperiment(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        var paramsJson = reader.IsDBNull(reader.GetOrdinal("hyperparameters")) ? "{}" : reader.GetString(reader.GetOrdinal("hyperparameters"));
        var metricsJson = reader.IsDBNull(reader.GetOrdinal("metrics")) ? "{}" : reader.GetString(reader.GetOrdinal("metrics"));

        return new Experiment
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            ProjectId = Guid.Parse(reader.GetString(reader.GetOrdinal("project_id"))),
            DatasetId = Guid.Parse(reader.GetString(reader.GetOrdinal("dataset_id"))),
            Status = Enum.Parse<ExperimentStatus>(reader.GetString(reader.GetOrdinal("status"))),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            StartedAt = reader.IsDBNull(reader.GetOrdinal("started_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("started_at"))),
            CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("completed_at"))),
            ComputeTarget = Enum.Parse<ComputeTarget>(reader.GetString(reader.GetOrdinal("compute_target"))),
            Hyperparameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson, _jsonOptions)?.AsReadOnly() ?? new Dictionary<string, object>().AsReadOnly(),
            Metrics = JsonSerializer.Deserialize<Dictionary<string, double>>(metricsJson, _jsonOptions)?.AsReadOnly() ?? new Dictionary<string, double>().AsReadOnly(),
            AzureMlExperimentId = reader.IsDBNull(reader.GetOrdinal("azure_ml_experiment_id")) ? null : reader.GetString(reader.GetOrdinal("azure_ml_experiment_id"))
        };
    }
}

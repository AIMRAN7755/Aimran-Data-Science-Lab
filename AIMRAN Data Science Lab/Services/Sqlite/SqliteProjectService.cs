using System.Text.Json;
using AimranDataScienceLab.Engine.Data;
using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Sqlite;

/// <summary>
/// SQLite-backed implementation of the project service.
/// </summary>
internal sealed class SqliteProjectService : IProjectService
{
    private readonly SqliteConnectionFactory _db;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SqliteProjectService(SqliteConnectionFactory db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM projects ORDER BY created_at DESC;";

        var projects = new List<Project>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            projects.Add(MapProject(reader));
        }

        return Task.FromResult<IReadOnlyList<Project>>(projects);
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM projects WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        using var reader = cmd.ExecuteReader();
        return Task.FromResult(reader.Read() ? MapProject(reader) : null);
    }

    public Task<Project> CreateAsync(string name, string localPath, string? description = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);
        cancellationToken.ThrowIfCancellationRequested();

        var project = new Project
        {
            Name = name,
            Description = description,
            LocalPath = localPath,
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO projects (id, name, description, local_path, status, created_at, tags)
            VALUES ($id, $name, $desc, $path, $status, $created, $tags);
            """;
        cmd.Parameters.AddWithValue("$id", project.Id.ToString());
        cmd.Parameters.AddWithValue("$name", project.Name);
        cmd.Parameters.AddWithValue("$desc", (object?)project.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$path", project.LocalPath);
        cmd.Parameters.AddWithValue("$status", project.Status.ToString());
        cmd.Parameters.AddWithValue("$created", project.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(project.Tags, _jsonOptions));
        cmd.ExecuteNonQuery();

        return Task.FromResult(project);
    }

    public Task<Project> UpdateAsync(Guid id, string? name = null, string? description = null, ProjectStatus? status = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = GetByIdAsync(id, cancellationToken).Result
            ?? throw new InvalidOperationException($"Project {id} not found.");

        var updated = existing with
        {
            Name = name ?? existing.Name,
            Description = description ?? existing.Description,
            Status = status ?? existing.Status,
            UpdatedAt = DateTime.UtcNow
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE projects SET name = $name, description = $desc, status = $status, updated_at = $updated
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$name", updated.Name);
        cmd.Parameters.AddWithValue("$desc", (object?)updated.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$status", updated.Status.ToString());
        cmd.Parameters.AddWithValue("$updated", updated.UpdatedAt?.ToString("O") ?? DBNull.Value.ToString());
        cmd.ExecuteNonQuery();

        return Task.FromResult(updated);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM projects WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<Project> ConnectToAzureAsync(Guid projectId, AzureProjectConfig azureConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(azureConfig);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = GetByIdAsync(projectId, cancellationToken).Result
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var updated = existing with
        {
            AzureConfig = azureConfig with { IsConnected = true },
            UpdatedAt = DateTime.UtcNow
        };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE projects SET
                azure_subscription_id = $sub, azure_resource_group = $rg, azure_workspace_name = $ws,
                azure_storage_account = $sa, azure_container_name = $cn, azure_is_connected = 1,
                updated_at = $updated
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", projectId.ToString());
        cmd.Parameters.AddWithValue("$sub", azureConfig.SubscriptionId);
        cmd.Parameters.AddWithValue("$rg", azureConfig.ResourceGroupName);
        cmd.Parameters.AddWithValue("$ws", azureConfig.WorkspaceName);
        cmd.Parameters.AddWithValue("$sa", (object?)azureConfig.StorageAccountName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cn", (object?)azureConfig.ContainerName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return Task.FromResult(updated);
    }

    public Task<Project> DisconnectFromAzureAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = GetByIdAsync(projectId, cancellationToken).Result
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var updated = existing with { AzureConfig = null, UpdatedAt = DateTime.UtcNow };

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE projects SET
                azure_subscription_id = NULL, azure_resource_group = NULL, azure_workspace_name = NULL,
                azure_storage_account = NULL, azure_container_name = NULL, azure_is_connected = 0,
                updated_at = $updated
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", projectId.ToString());
        cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return Task.FromResult(updated);
    }

    private Project MapProject(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        var azureConnected = reader.GetInt32(reader.GetOrdinal("azure_is_connected")) == 1;
        AzureProjectConfig? azureConfig = null;

        if (azureConnected)
        {
            var subId = reader.IsDBNull(reader.GetOrdinal("azure_subscription_id")) ? "" : reader.GetString(reader.GetOrdinal("azure_subscription_id"));
            var rg = reader.IsDBNull(reader.GetOrdinal("azure_resource_group")) ? "" : reader.GetString(reader.GetOrdinal("azure_resource_group"));
            var ws = reader.IsDBNull(reader.GetOrdinal("azure_workspace_name")) ? "" : reader.GetString(reader.GetOrdinal("azure_workspace_name"));

            azureConfig = new AzureProjectConfig
            {
                SubscriptionId = subId,
                ResourceGroupName = rg,
                WorkspaceName = ws,
                StorageAccountName = reader.IsDBNull(reader.GetOrdinal("azure_storage_account")) ? null : reader.GetString(reader.GetOrdinal("azure_storage_account")),
                ContainerName = reader.IsDBNull(reader.GetOrdinal("azure_container_name")) ? null : reader.GetString(reader.GetOrdinal("azure_container_name")),
                IsConnected = true
            };
        }

        var tagsJson = reader.IsDBNull(reader.GetOrdinal("tags")) ? "[]" : reader.GetString(reader.GetOrdinal("tags"));
        var tags = JsonSerializer.Deserialize<List<string>>(tagsJson, _jsonOptions) ?? [];

        return new Project
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            LocalPath = reader.GetString(reader.GetOrdinal("local_path")),
            Status = Enum.Parse<ProjectStatus>(reader.GetString(reader.GetOrdinal("status"))),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at"))),
            Tags = tags,
            AzureConfig = azureConfig
        };
    }
}

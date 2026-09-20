using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services.Local;

/// <summary>
/// Local in-memory implementation of the project service.
/// </summary>
internal sealed class LocalProjectService : IProjectService
{
    private readonly List<Project> _projects = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Project>>(_projects.ToList());
        }
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));
        }
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

        lock (_lock)
        {
            _projects.Add(project);
        }

        return Task.FromResult(project);
    }

    public Task<Project> UpdateAsync(Guid id, string? name = null, string? description = null, ProjectStatus? status = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var index = _projects.FindIndex(p => p.Id == id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Project {id} not found.");
            }

            var existing = _projects[index];
            var updated = existing with
            {
                Name = name ?? existing.Name,
                Description = description ?? existing.Description,
                Status = status ?? existing.Status,
                UpdatedAt = DateTime.UtcNow
            };

            _projects[index] = updated;
            return Task.FromResult(updated);
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _projects.RemoveAll(p => p.Id == id);
        }
        return Task.CompletedTask;
    }

    public Task<Project> ConnectToAzureAsync(Guid projectId, AzureProjectConfig azureConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(azureConfig);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var index = _projects.FindIndex(p => p.Id == projectId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Project {projectId} not found.");
            }

            var updated = _projects[index] with
            {
                AzureConfig = azureConfig with { IsConnected = true },
                UpdatedAt = DateTime.UtcNow
            };

            _projects[index] = updated;
            return Task.FromResult(updated);
        }
    }

    public Task<Project> DisconnectFromAzureAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var index = _projects.FindIndex(p => p.Id == projectId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Project {projectId} not found.");
            }

            var updated = _projects[index] with
            {
                AzureConfig = null,
                UpdatedAt = DateTime.UtcNow
            };

            _projects[index] = updated;
            return Task.FromResult(updated);
        }
    }
}

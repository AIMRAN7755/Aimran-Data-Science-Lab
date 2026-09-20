using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing projects in the DS-Workbench.
/// </summary>
public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project> CreateAsync(string name, string localPath, string? description = null, CancellationToken cancellationToken = default);
    Task<Project> UpdateAsync(Guid id, string? name = null, string? description = null, ProjectStatus? status = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project> ConnectToAzureAsync(Guid projectId, AzureProjectConfig azureConfig, CancellationToken cancellationToken = default);
    Task<Project> DisconnectFromAzureAsync(Guid projectId, CancellationToken cancellationToken = default);
}

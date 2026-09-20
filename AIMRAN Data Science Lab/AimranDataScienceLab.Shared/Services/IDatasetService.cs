using AIMRAN_Data_Science_Lab.Models;

namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Service for managing datasets in the DS-Workbench.
/// </summary>
public interface IDatasetService
{
    Task<IReadOnlyList<Dataset>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Dataset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Dataset> CreateAsync(string name, string filePath, string? description = null, CancellationToken cancellationToken = default);
    Task<Dataset> ImportFromFileAsync(string sourceFilePath, string? name = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Dataset> CreateVersionAsync(Guid datasetId, string newFilePath, CancellationToken cancellationToken = default);
    Task<DatasetPreview> GetPreviewAsync(Guid id, int rowCount = 100, CancellationToken cancellationToken = default);
}

/// <summary>
/// Preview data for a dataset.
/// </summary>
public record DatasetPreview
{
    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];
    public int TotalRows { get; init; }
}

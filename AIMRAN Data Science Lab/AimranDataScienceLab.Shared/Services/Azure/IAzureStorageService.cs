using AIMRAN_Data_Science_Lab.Models;
using AIMRAN_Data_Science_Lab.Models.Azure;

namespace AIMRAN_Data_Science_Lab.Services.Azure;

/// <summary>
/// Service for Azure Blob Storage operations.
/// </summary>
public interface IAzureStorageService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task<AzureOperationResult> ConfigureAsync(AzureStorageConfig config, CancellationToken cancellationToken = default);
    Task<AzureOperationResult> UploadDatasetAsync(Dataset dataset, Stream dataStream, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadDatasetAsync(string blobName, CancellationToken cancellationToken = default);
    Task<AzureOperationResult> DeleteDatasetAsync(string blobName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListDatasetsAsync(string? prefix = null, CancellationToken cancellationToken = default);
    Task<AzureOperationResult> UploadModelAsync(MlModel model, Stream modelStream, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadModelAsync(string blobName, CancellationToken cancellationToken = default);
}

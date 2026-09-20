using AIMRAN_Data_Science_Lab.Models.Azure;

namespace AIMRAN_Data_Science_Lab.Services.Azure;

/// <summary>
/// Service for managing Azure configuration and authentication.
/// </summary>
public interface IAzureConfigService
{
    Task<AzureConfig> GetConfigAsync(CancellationToken cancellationToken = default);
    Task<AzureConfig> SaveConfigAsync(AzureConfig config, CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
    Task<AzureOperationResult> AuthenticateAsync(CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
    bool IsAuthenticated { get; }
}

namespace AIMRAN_Data_Science_Lab.Models.Azure;

/// <summary>
/// Global Azure configuration for the DS-Workbench.
/// </summary>
public record AzureConfig
{
    public string? TenantId { get; init; }
    public string? ClientId { get; init; }
    public string? SubscriptionId { get; init; }
    public AzureStorageConfig? Storage { get; init; }
    public AzureMlConfig? MachineLearning { get; init; }
    public bool IsConfigured => !string.IsNullOrEmpty(SubscriptionId);
}

/// <summary>
/// Azure Blob Storage configuration.
/// </summary>
public record AzureStorageConfig
{
    public required string AccountName { get; init; }
    public required string ContainerName { get; init; }
    public string? ConnectionString { get; init; }
    public string BlobEndpoint => $"https://{AccountName}.blob.core.windows.net";
}

/// <summary>
/// Azure Machine Learning workspace configuration.
/// </summary>
public record AzureMlConfig
{
    public required string SubscriptionId { get; init; }
    public required string ResourceGroupName { get; init; }
    public required string WorkspaceName { get; init; }
    public string? DefaultComputeTarget { get; init; }
    public string? DefaultDatastore { get; init; }
}

/// <summary>
/// Result of an Azure operation.
/// </summary>
public record AzureOperationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ResourceId { get; init; }
    public string? ResourceUrl { get; init; }

    public static AzureOperationResult Success(string? resourceId = null, string? resourceUrl = null)
        => new() { IsSuccess = true, ResourceId = resourceId, ResourceUrl = resourceUrl };

    public static AzureOperationResult Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}

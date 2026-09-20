namespace AIMRAN_Data_Science_Lab.Services;

/// <summary>
/// Secure storage for secrets such as API keys, encryption keys, and credentials.
/// Local mode uses platform-protected encryption; cloud mode delegates to Azure Key Vault.
/// </summary>
public interface ISecretStore
{
    /// <summary>Store a secret under the given key.</summary>
    Task SetAsync(string key, string value, CancellationToken ct = default);

    /// <summary>Retrieve a secret by key, or null if not found.</summary>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Delete a secret by key.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>List all secret key names (not values).</summary>
    Task<IReadOnlyList<string>> ListKeysAsync(CancellationToken ct = default);
}

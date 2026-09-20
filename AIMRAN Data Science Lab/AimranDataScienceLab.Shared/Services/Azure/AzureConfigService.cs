using System.Text.Json;
using AIMRAN_Data_Science_Lab.Models.Azure;

namespace AIMRAN_Data_Science_Lab.Services.Azure;

/// <summary>
/// Service for managing Azure configuration and authentication.
/// Uses file-based storage for configuration persistence.
/// </summary>
public sealed class AzureConfigService : IAzureConfigService
{
    private readonly string _configFilePath;
    private AzureConfig? _cachedConfig;
    private bool _isAuthenticated;

    public AzureConfigService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(appDataPath, "AIMRAN-DS-Workbench");
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, "azure-config.json");
    }

    public bool IsAuthenticated => _isAuthenticated;

    public async Task<AzureConfig> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedConfig is not null)
        {
            return _cachedConfig;
        }

        if (!File.Exists(_configFilePath))
        {
            return new AzureConfig();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            _cachedConfig = JsonSerializer.Deserialize<AzureConfig>(json) ?? new AzureConfig();
            return _cachedConfig;
        }
        catch (JsonException)
        {
            return new AzureConfig();
        }
    }

    public async Task<AzureConfig> SaveConfigAsync(AzureConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configFilePath, json, cancellationToken);

        _cachedConfig = config;
        return config;
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigAsync(cancellationToken);
        return config.IsConfigured && _isAuthenticated;
    }

    public async Task<AzureOperationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigAsync(cancellationToken);

        if (!config.IsConfigured)
        {
            return AzureOperationResult.Failure("Azure configuration is not set up. Please configure subscription details first.");
        }

        // Placeholder - real implementation would use Azure.Identity
        // DefaultAzureCredential or InteractiveBrowserCredential
        _isAuthenticated = true;
        return AzureOperationResult.Success();
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _isAuthenticated = false;
        return Task.CompletedTask;
    }
}

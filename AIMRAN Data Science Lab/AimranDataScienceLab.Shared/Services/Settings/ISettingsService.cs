namespace AIMRAN_Data_Science_Lab.Services.Settings;

/// <summary>
/// Settings categories and their configuration
/// </summary>
public record SettingsCategory
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "";
    public string Description { get; init; } = "";
    public int Order { get; init; }
}

/// <summary>
/// User settings model
/// </summary>
public record UserSettings
{
    public string UserId { get; set; } = "";
    public string Theme { get; set; } = "light";
    public bool EnableNotifications { get; set; } = true;
    public bool EnableEmailDigest { get; set; } = true;
    public string Language { get; set; } = "en";
    public string DateFormat { get; set; } = "MM/dd/yyyy";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Service for managing user settings
/// </summary>
public interface ISettingsService
{
    Task<UserSettings> GetSettingsAsync(string userId);
    Task SaveSettingsAsync(UserSettings settings);
    Task<IEnumerable<SettingsCategory>> GetCategoriesAsync();
}

/// <summary>
/// In-memory implementation of settings service
/// </summary>
public class InMemorySettingsService : ISettingsService
{
    private readonly Dictionary<string, UserSettings> _settingsStore = new();

    public Task<UserSettings> GetSettingsAsync(string userId)
    {
        if (_settingsStore.TryGetValue(userId, out var settings))
        {
            return Task.FromResult(settings);
        }

        var defaultSettings = new UserSettings
        {
            UserId = userId,
            Theme = "light",
            EnableNotifications = true,
            EnableEmailDigest = true,
            Language = "en",
            DateFormat = "MM/dd/yyyy",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _settingsStore[userId] = defaultSettings;
        return Task.FromResult(defaultSettings);
    }

    public Task SaveSettingsAsync(UserSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _settingsStore[settings.UserId] = settings;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<SettingsCategory>> GetCategoriesAsync()
    {
        var categories = new[]
        {
            new SettingsCategory
            {
                Id = "profile",
                Name = "User Profile",
                Icon = "person",
                Description = "Manage your personal information",
                Order = 1
            },
            new SettingsCategory
            {
                Id = "preferences",
                Name = "Preferences",
                Icon = "tune",
                Description = "Customize your experience",
                Order = 2
            },
            new SettingsCategory
            {
                Id = "tools",
                Name = "Data Science Tools",
                Icon = "settings_suggest",
                Description = "Configure tools and integrations",
                Order = 3
            },
            new SettingsCategory
            {
                Id = "api",
                Name = "API Keys",
                Icon = "vpn_key",
                Description = "Manage your API credentials",
                Order = 4
            },
            new SettingsCategory
            {
                Id = "theme",
                Name = "Appearance",
                Icon = "palette",
                Description = "Customize look and feel",
                Order = 5
            },
            new SettingsCategory
            {
                Id = "notifications",
                Name = "Notifications",
                Icon = "notifications",
                Description = "Manage notification preferences",
                Order = 6
            },
            new SettingsCategory
            {
                Id = "privacy",
                Name = "Privacy & Security",
                Icon = "security",
                Description = "Control privacy and security options",
                Order = 7
            }
        };

        return Task.FromResult(categories.AsEnumerable());
    }
}

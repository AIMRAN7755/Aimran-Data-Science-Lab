using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// Platform-secured secret store that persists encrypted secrets to a JSON file
/// protected by a machine-scoped AES key (similar to DPAPI behaviour).
/// Suitable for local/desktop deployments. For cloud, swap with Azure Key Vault.
/// </summary>
internal sealed class LocalSecretStore : ISecretStore
{
    private readonly string _secretsFilePath;
    private readonly byte[] _encryptionKey;
    private readonly object _lock = new();

    public LocalSecretStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIMRAN-DataScience");
        Directory.CreateDirectory(dir);

        _secretsFilePath = Path.Combine(dir, ".secrets.enc");
        _encryptionKey = GetOrCreateEncryptionKey(dir);
    }

    public Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        lock (_lock)
        {
            var store = Load();
            store[key] = value;
            Save(store);
        }
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            var store = Load();
            return Task.FromResult(store.TryGetValue(key, out var value) ? value : null);
        }
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            var store = Load();
            store.Remove(key);
            Save(store);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var store = Load();
            return Task.FromResult<IReadOnlyList<string>>(store.Keys.ToList());
        }
    }

    // ── Encryption helpers ───────────────────────────────────────────

    private Dictionary<string, string> Load()
    {
        if (!File.Exists(_secretsFilePath))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var ciphertext = File.ReadAllBytes(_secretsFilePath);
        var json = Decrypt(ciphertext);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private void Save(Dictionary<string, string> store)
    {
        var json = JsonSerializer.Serialize(store);
        var ciphertext = Encrypt(json);
        File.WriteAllBytes(_secretsFilePath, ciphertext);
    }

    private byte[] Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + encrypted.Length];
        aes.IV.CopyTo(result, 0);
        encrypted.CopyTo(result, aes.IV.Length);
        return result;
    }

    private string Decrypt(byte[] ciphertext)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(ciphertext, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var encrypted = new byte[ciphertext.Length - iv.Length];
        Array.Copy(ciphertext, iv.Length, encrypted, 0, encrypted.Length);
        var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        return Encoding.UTF8.GetString(decrypted);
    }

    private static byte[] GetOrCreateEncryptionKey(string dir)
    {
        var keyFile = Path.Combine(dir, ".secret-key");
        if (File.Exists(keyFile))
        {
            var existing = File.ReadAllBytes(keyFile);
            if (existing.Length == 32)
                return existing;
        }

        var key = RandomNumberGenerator.GetBytes(32); // AES-256
        File.WriteAllBytes(keyFile, key);
        return key;
    }
}

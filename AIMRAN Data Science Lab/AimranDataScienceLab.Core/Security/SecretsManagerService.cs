using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.Security
{
    /// <summary>
    /// Secret metadata for tracking and auditing.
    /// </summary>
    public class Secret
    {
        public string SecretId { get; set; } = "";
        public string SecretName { get; set; } = "";
        public string SecretType { get; set; } = ""; // DatabasePassword, APIKey, Certificate, etc.
        public string? SecretValue { get; set; }
        public bool IsEncrypted { get; set; }
        public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
        public string Owner { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Status { get; set; } = "Active"; // Active, Expired, Rotated, Revoked
        public int Version { get; set; }
        public List<string> AllowedUsers { get; set; } = new();
        public List<SecretAccessLog> AccessLogs { get; set; } = new();
    }

    public class SecretAccessLog
    {
        public string AccessId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Action { get; set; } = ""; // Read, Update, Delete
        public DateTime AccessTime { get; set; }
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// Secrets Manager for secure credential storage and rotation.
    /// </summary>
    public interface ISecretsManagerService
    {
        /// <summary>
        /// Creates a new secret.
        /// </summary>
        Task<string> CreateSecretAsync(Secret secret, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a secret value.
        /// </summary>
        Task<string?> GetSecretAsync(string secretId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a secret (creates new version).
        /// </summary>
        Task<string> UpdateSecretAsync(string secretId, string newValue, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rotates a secret.
        /// </summary>
        Task<bool> RotateSecretAsync(string secretId, string newValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a secret.
        /// </summary>
        Task<bool> DeleteSecretAsync(string secretId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets secret access history.
        /// </summary>
        Task<List<SecretAccessLog>> GetAccessHistoryAsync(string secretId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Grants access to a secret for a user.
        /// </summary>
        Task<bool> GrantAccessAsync(string secretId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes access to a secret.
        /// </summary>
        Task<bool> RevokeAccessAsync(string secretId, string userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of Secrets Manager service.
    /// </summary>
    public class SecretsManagerService : ISecretsManagerService
    {
        private readonly ILogger<SecretsManagerService> _logger;
        private readonly Dictionary<string, Secret> _secrets = new();
        private readonly Dictionary<string, List<Secret>> _secretVersions = new();

        public SecretsManagerService(ILogger<SecretsManagerService> logger)
        {
            _logger = logger;
        }

        public async Task<string> CreateSecretAsync(Secret secret, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var secretId = secret.SecretId ?? Guid.NewGuid().ToString();
                secret.SecretId = secretId;
                secret.CreatedAt = DateTime.UtcNow;
                secret.Owner = userId;
                secret.Version = 1;

                // Encrypt secret value
                secret.SecretValue = EncryptSecret(secret.SecretValue ?? "");
                secret.IsEncrypted = true;

                _secrets[secretId] = secret;
                _secretVersions[secretId] = new List<Secret> { secret };

                _logger.LogInformation("Created secret: {SecretName} ({SecretId}) by {UserId}", 
                    secret.SecretName, secretId, userId);

                return secretId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret");
                throw;
            }
        }

        public async Task<string?> GetSecretAsync(string secretId, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_secrets.TryGetValue(secretId, out var secret))
                {
                    // Check access
                    if (!secret.AllowedUsers.Contains(userId) && secret.Owner != userId)
                    {
                        _logger.LogWarning("Unauthorized access attempt to secret {SecretId} by {UserId}", secretId, userId);
                        LogAccessAttempt(secret, userId, "Read", "Access Denied");
                        throw new UnauthorizedAccessException("Access denied to this secret");
                    }

                    LogAccessAttempt(secret, userId, "Read", null);
                    var decryptedValue = DecryptSecret(secret.SecretValue ?? "");
                    _logger.LogInformation("Retrieved secret {SecretId} by {UserId}", secretId, userId);

                    return decryptedValue;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret");
                throw;
            }
        }

        public async Task<string> UpdateSecretAsync(string secretId, string newValue, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_secrets.TryGetValue(secretId, out var secret))
                {
                    if (secret.Owner != userId)
                    {
                        _logger.LogWarning("Unauthorized update attempt to secret {SecretId} by {UserId}", secretId, userId);
                        throw new UnauthorizedAccessException("Only secret owner can update");
                    }

                    var newSecret = new Secret
                    {
                        SecretId = secretId,
                        SecretName = secret.SecretName,
                        SecretType = secret.SecretType,
                        SecretValue = EncryptSecret(newValue),
                        IsEncrypted = true,
                        Owner = userId,
                        CreatedAt = DateTime.UtcNow,
                        Version = secret.Version + 1,
                        AllowedUsers = secret.AllowedUsers
                    };

                    _secrets[secretId] = newSecret;
                    _secretVersions[secretId].Add(newSecret);

                    LogAccessAttempt(newSecret, userId, "Update", null);
                    _logger.LogInformation("Updated secret {SecretId} (v{Version}) by {UserId}", secretId, newSecret.Version, userId);

                    return secretId;
                }

                throw new KeyNotFoundException($"Secret {secretId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret");
                throw;
            }
        }

        public async Task<bool> RotateSecretAsync(string secretId, string newValue, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_secrets.TryGetValue(secretId, out var secret))
                {
                    secret.SecretValue = EncryptSecret(newValue);
                    secret.Version++;
                    secret.Status = "Rotated";

                    _secretVersions[secretId].Add(secret);
                    _logger.LogInformation("Rotated secret {SecretId} to v{Version}", secretId, secret.Version);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating secret");
                throw;
            }
        }

        public async Task<bool> DeleteSecretAsync(string secretId, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_secrets.TryGetValue(secretId, out var secret))
                {
                    if (secret.Owner != userId)
                    {
                        _logger.LogWarning("Unauthorized delete attempt to secret {SecretId} by {UserId}", secretId, userId);
                        throw new UnauthorizedAccessException("Only secret owner can delete");
                    }

                    secret.Status = "Revoked";
                    LogAccessAttempt(secret, userId, "Delete", null);
                    _logger.LogInformation("Deleted secret {SecretId} by {UserId}", secretId, userId);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret");
                throw;
            }
        }

        public async Task<List<SecretAccessLog>> GetAccessHistoryAsync(string secretId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_secrets.TryGetValue(secretId, out var secret))
                {
                    _logger.LogInformation("Retrieved access history for secret {SecretId}", secretId);
                    return secret.AccessLogs;
                }

                return new List<SecretAccessLog>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving access history");
                throw;
            }
        }

        public async Task<bool> GrantAccessAsync(string secretId, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_secrets.TryGetValue(secretId, out var secret))
                {
                    if (!secret.AllowedUsers.Contains(userId))
                    {
                        secret.AllowedUsers.Add(userId);
                        _logger.LogInformation("Granted access to secret {SecretId} for user {UserId}", secretId, userId);
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting access");
                throw;
            }
        }

        public async Task<bool> RevokeAccessAsync(string secretId, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_secrets.TryGetValue(secretId, out var secret))
                {
                    if (secret.AllowedUsers.Remove(userId))
                    {
                        _logger.LogInformation("Revoked access to secret {SecretId} for user {UserId}", secretId, userId);
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking access");
                throw;
            }
        }

        private string EncryptSecret(string value)
        {
            // Simplified encryption - in production use proper encryption
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
        }

        private string DecryptSecret(string encryptedValue)
        {
            // Simplified decryption - in production use proper decryption
            try
            {
                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedValue));
            }
            catch
            {
                return encryptedValue;
            }
        }

        private void LogAccessAttempt(Secret secret, string userId, string action, string? failureReason)
        {
            secret.AccessLogs.Add(new SecretAccessLog
            {
                AccessId = Guid.NewGuid().ToString(),
                UserId = userId,
                Action = action,
                AccessTime = DateTime.UtcNow,
                FailureReason = failureReason
            });
        }
    }
}

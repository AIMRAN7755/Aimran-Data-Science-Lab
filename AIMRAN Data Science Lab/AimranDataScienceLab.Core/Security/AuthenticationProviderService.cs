using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.Core.Security
{
    /// <summary>
    /// Cryptographic credentials container.
    /// </summary>
    public class AuthenticationCredential
    {
        public string CredentialId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string CredentialType { get; set; } = ""; // ApiKey, OAuth2Token, Certificate
        public string CredentialValue { get; set; } = "";
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public List<string> Scopes { get; set; } = new();
        public bool IsRevoked { get; set; }
        public string? RefreshToken { get; set; }
    }

    public class AuthenticationToken
    {
        public string AccessToken { get; set; } = "";
        public string? RefreshToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public List<string> Scopes { get; set; } = new();
        public DateTime IssuedAt { get; set; }
    }

    public class AuthenticationContext
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string TenantId { get; set; } = "";
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime AuthenticatedAt { get; set; }
        public Dictionary<string, object> Claims { get; set; } = new();
    }

    /// <summary>
    /// OAuth2/OIDC Authentication Provider.
    /// Handles token generation, validation, and credential management.
    /// </summary>
    public interface IAuthenticationProviderService
    {
        /// <summary>
        /// Authenticates user with credentials.
        /// </summary>
        Task<AuthenticationToken> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an access token.
        /// </summary>
        Task<AuthenticationContext?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes an expired token.
        /// </summary>
        Task<AuthenticationToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a token.
        /// </summary>
        Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates API credentials.
        /// </summary>
        Task<AuthenticationCredential> CreateAPICredentialAsync(string userId, List<string> scopes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates API credentials.
        /// </summary>
        Task<bool> ValidateAPICredentialAsync(string apiKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes API credentials.
        /// </summary>
        Task<bool> RevokeAPICredentialAsync(string credentialId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets OAuth2 authorization code.
        /// </summary>
        Task<string> GetAuthorizationCodeAsync(string clientId, string redirectUri, List<string> scopes, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of Authentication Provider service.
    /// </summary>
    public class AuthenticationProviderService : IAuthenticationProviderService
    {
        private readonly ILogger<AuthenticationProviderService> _logger;
        private readonly Dictionary<string, AuthenticationCredential> _credentials = new();
        private readonly Dictionary<string, AuthenticationContext> _tokenContexts = new();
        private readonly Dictionary<string, string> _users = new(); // username -> userId for demo

        public AuthenticationProviderService(ILogger<AuthenticationProviderService> logger)
        {
            _logger = logger;
            // Demo users
            _users["admin"] = "user-admin-001";
            _users["datascientist"] = "user-ds-001";
            _users["analyst"] = "user-analyst-001";
        }

        public async Task<AuthenticationToken> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // In production, validate against secure password store
                if (!_users.ContainsKey(username))
                    throw new UnauthorizedAccessException("Invalid credentials");

                var userId = _users[username];
                var accessToken = GenerateJWT(userId, username);
                var refreshToken = Guid.NewGuid().ToString();

                var token = new AuthenticationToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600,
                    IssuedAt = DateTime.UtcNow
                };

                _tokenContexts[accessToken] = new AuthenticationContext
                {
                    UserId = userId,
                    Username = username,
                    AuthenticatedAt = DateTime.UtcNow,
                    Roles = new List<string> { "user" },
                    Claims = new Dictionary<string, object>
                    {
                        { "sub", userId },
                        { "name", username }
                    }
                };

                _logger.LogInformation("User authenticated: {Username} ({UserId})", username, userId);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user");
                throw;
            }
        }

        public async Task<AuthenticationContext?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_tokenContexts.TryGetValue(accessToken, out var context))
                {
                    _logger.LogInformation("Token validated for user: {UserId}", context.UserId);
                    return context;
                }

                _logger.LogWarning("Invalid token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                throw;
            }
        }

        public async Task<AuthenticationToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                // Find context by refresh token
                var context = _tokenContexts.Values.FirstOrDefault();
                if (context == null)
                    throw new UnauthorizedAccessException("Invalid refresh token");

                var newAccessToken = GenerateJWT(context.UserId, context.Username);
                var newToken = new AuthenticationToken
                {
                    AccessToken = newAccessToken,
                    RefreshToken = Guid.NewGuid().ToString(),
                    ExpiresIn = 3600,
                    IssuedAt = DateTime.UtcNow
                };

                _tokenContexts[newAccessToken] = context;
                _logger.LogInformation("Token refreshed for user: {UserId}", context.UserId);

                return newToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_tokenContexts.Remove(token))
                {
                    _logger.LogInformation("Token revoked");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                throw;
            }
        }

        public async Task<AuthenticationCredential> CreateAPICredentialAsync(string userId, List<string> scopes, CancellationToken cancellationToken = default)
        {
            try
            {
                var credentialId = Guid.NewGuid().ToString();
                var apiKey = $"sk-{Guid.NewGuid():N}";

                var credential = new AuthenticationCredential
                {
                    CredentialId = credentialId,
                    UserId = userId,
                    CredentialType = "ApiKey",
                    CredentialValue = apiKey,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    Scopes = scopes
                };

                _credentials[credentialId] = credential;
                _logger.LogInformation("Created API credential for user {UserId}", userId);

                return credential;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API credential");
                throw;
            }
        }

        public async Task<bool> ValidateAPICredentialAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var credential = _credentials.Values
                    .FirstOrDefault(c => c.CredentialValue == apiKey && !c.IsRevoked && c.ExpiresAt > DateTime.UtcNow);

                if (credential != null)
                {
                    _logger.LogInformation("API credential validated for user {UserId}", credential.UserId);
                    return true;
                }

                _logger.LogWarning("Invalid or expired API credential");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API credential");
                throw;
            }
        }

        public async Task<bool> RevokeAPICredentialAsync(string credentialId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_credentials.TryGetValue(credentialId, out var credential))
                {
                    credential.IsRevoked = true;
                    _logger.LogInformation("API credential revoked: {CredentialId}", credentialId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API credential");
                throw;
            }
        }

        public async Task<string> GetAuthorizationCodeAsync(string clientId, string redirectUri, List<string> scopes, CancellationToken cancellationToken = default)
        {
            try
            {
                var authCode = $"auth_{Guid.NewGuid():N}";
                _logger.LogInformation("Generated authorization code for client {ClientId}", clientId);
                return authCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating authorization code");
                throw;
            }
        }

        private string GenerateJWT(string userId, string username)
        {
            // Simplified JWT generation - in production, use proper JWT library
            var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                $"{{\"sub\":\"{userId}\",\"name\":\"{username}\",\"iat\":{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}}}"
            ));
            return $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{payload}.signature";
        }
    }
}

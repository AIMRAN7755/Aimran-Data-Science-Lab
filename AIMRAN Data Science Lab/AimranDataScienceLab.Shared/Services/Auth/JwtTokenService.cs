using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AIMRAN_Data_Science_Lab.Models.Auth;
using Microsoft.IdentityModel.Tokens;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// Creates and validates JWT tokens for the AIMRAN authentication system.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Generate a signed JWT for the given user.</summary>
    string GenerateToken(AimranUser user);

    /// <summary>Validate a token and return claims, or null if invalid.</summary>
    ClaimsPrincipal? ValidateToken(string token);
}

/// <inheritdoc />
internal sealed class JwtTokenService : IJwtTokenService
{
    private const int TokenExpirationMinutes = 480; // 8-hour session
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(string? signingSecret = null)
    {
        // In production the secret should come from Azure Key Vault or secure storage.
        // For local development a machine-scoped key is generated on first run.
        var secret = signingSecret ?? GetOrCreateLocalSecret();
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _issuer = "AimranDataScienceLab";
        _audience = "AimranDataScienceLab";
    }

    public string GenerateToken(AimranUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    /// <summary>
    /// Generate a local machine-scoped signing secret persisted to the app data folder.
    /// </summary>
    private static string GetOrCreateLocalSecret()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIMRAN-DataScience");
        Directory.CreateDirectory(dir);

        var keyFile = Path.Combine(dir, ".jwt-key");
        if (File.Exists(keyFile))
        {
            var existing = File.ReadAllText(keyFile).Trim();
            if (existing.Length >= 32)
                return existing;
        }

        // 64-char random key (256-bit)
        var key = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        File.WriteAllText(keyFile, key);
        return key;
    }
}

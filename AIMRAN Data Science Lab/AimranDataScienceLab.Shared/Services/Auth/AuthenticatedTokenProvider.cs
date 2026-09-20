using AimranDataScienceLab.Gateway.Clients;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// Provides the current user's JWT to outgoing engine API requests
/// so Python/R/Rust backends can verify the caller's identity.
/// </summary>
internal sealed class AuthenticatedTokenProvider : IEngineTokenProvider
{
    private readonly IAuthService _auth;
    private readonly IJwtTokenService _jwt;

    public AuthenticatedTokenProvider(IAuthService auth, IJwtTokenService jwt)
    {
        _auth = auth;
        _jwt = jwt;
    }

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var user = await _auth.GetCurrentUserAsync(cancellationToken);
        if (user is null)
            return null;

        return _jwt.GenerateToken(user);
    }
}

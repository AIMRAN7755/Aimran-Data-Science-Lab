using System.Security.Claims;
using AIMRAN_Data_Science_Lab.Services.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace AIMRAN_Data_Science_Lab.Services.Auth;

/// <summary>
/// Blazor <see cref="AuthenticationStateProvider"/> backed by the AIMRAN
/// <see cref="IAuthService"/>. Notifies the component tree whenever the
/// user logs in or out.
/// </summary>
internal sealed class AimranAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IAuthService _auth;
    private readonly IJwtTokenService _jwt;

    public AimranAuthStateProvider(IAuthService auth, IJwtTokenService jwt)
    {
        _auth = auth;
        _jwt = jwt;
        _auth.AuthStateChanged += OnAuthStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await _auth.GetCurrentUserAsync();
        if (user is null)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        // Build claims from the current user token via JWT validation
        var token = (await Task.Run(() =>
        {
            // Generate a fresh token so claims are always up-to-date
            return _jwt.GenerateToken(user);
        }));

        var principal = _jwt.ValidateToken(token);
        if (principal is null)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        return new AuthenticationState(principal);
    }

    private void OnAuthStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() =>
        _auth.AuthStateChanged -= OnAuthStateChanged;
}

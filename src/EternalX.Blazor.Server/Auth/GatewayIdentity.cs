using System.Security.Claims;

namespace EternalX.Blazor.Server.Auth;

/// <summary>
/// Pure gateway identity rules (EternalSocial proxy SSO). No local OIDC.
/// Trust requires exact match of configured GATEWAY_KEY to the proxy-supplied
/// X-Gateway-Key, then builds claims from X-Auth-* headers.
/// </summary>
public static class GatewayIdentity
{
    public const string AuthenticationType = GatewayAuthHandler.SchemeName;

    /// <summary>
    /// Returns a principal when the proxy key matches and a user id is present;
    /// otherwise null (anonymous / no authentication result).
    /// </summary>
    public static ClaimsPrincipal? TryCreatePrincipal(
        string? configuredGatewayKey,
        string? suppliedGatewayKey,
        string? userId,
        string? displayName,
        string? email)
    {
        if (string.IsNullOrWhiteSpace(configuredGatewayKey))
            return null;

        if (string.IsNullOrEmpty(suppliedGatewayKey) ||
            !string.Equals(suppliedGatewayKey, configuredGatewayKey, StringComparison.Ordinal))
            return null;

        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (!string.IsNullOrWhiteSpace(displayName))
            claims.Add(new Claim(ClaimTypes.Name, displayName));
        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(ClaimTypes.Email, email));

        var identity = new ClaimsIdentity(claims, AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}

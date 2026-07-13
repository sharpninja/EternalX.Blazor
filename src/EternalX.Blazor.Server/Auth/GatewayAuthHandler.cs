using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EternalX.Blazor.Server.Auth;

/// <summary>
/// EternalSocial gateway SSO (see docs/gateway-sso.md in the EternalReddit repo):
/// the gateway performs the Google OIDC sign-in for every Eternal site and forwards
/// the identity as X-Auth-* headers proven by the shared X-Gateway-Key. Active only
/// when GATEWAY_KEY is configured; headers are never trusted without the key.
/// </summary>
public sealed class GatewayAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Gateway";

    private readonly IConfiguration _config;

    public GatewayAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, IConfiguration config)
        : base(options, logger, encoder) => _config = config;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key = _config["GATEWAY_KEY"];
        if (string.IsNullOrEmpty(key) || Request.Headers["X-Gateway-Key"] != key)
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = Request.Headers["X-Auth-UserId"].ToString();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var name = Request.Headers["X-Auth-Name"].ToString();
        if (name.Length > 0) claims.Add(new Claim(ClaimTypes.Name, name));
        var email = Request.Headers["X-Auth-Email"].ToString();
        if (email.Length > 0) claims.Add(new Claim(ClaimTypes.Email, email));

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

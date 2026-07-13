using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EternalX.Blazor.Server.Auth;

/// <summary>
/// EternalSocial gateway SSO only (see EternalReddit docs/gateway-sso.md).
/// The proxy performs sign-in and forwards identity as X-Auth-* headers, proven
/// by the shared X-Gateway-Key (GATEWAY_KEY). Local OIDC is not supported.
/// </summary>
public sealed class GatewayAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Gateway";

    private readonly IConfiguration _config;

    public GatewayAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var principal = GatewayIdentity.TryCreatePrincipal(
            configuredGatewayKey: _config["GATEWAY_KEY"],
            suppliedGatewayKey: Request.Headers["X-Gateway-Key"].ToString(),
            userId: Request.Headers["X-Auth-UserId"].ToString(),
            displayName: Request.Headers["X-Auth-Name"].ToString(),
            email: Request.Headers["X-Auth-Email"].ToString());

        if (principal is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}

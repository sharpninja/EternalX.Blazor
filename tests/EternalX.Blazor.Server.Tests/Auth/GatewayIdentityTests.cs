using System.Security.Claims;
using EternalX.Blazor.Server.Auth;

namespace EternalX.Blazor.Server.Tests.Auth;

/// <summary>
/// TEST-CORE / FR-CORE-010: gateway proxy identity only; no local OIDC.
/// </summary>
public class GatewayIdentityTests
{
    private const string Key = "shared-gateway-secret";

    [Fact]
    public void TryCreatePrincipal_null_when_configured_key_missing()
    {
        var p = GatewayIdentity.TryCreatePrincipal(
            configuredGatewayKey: null,
            suppliedGatewayKey: Key,
            userId: "user-1",
            displayName: "Ada",
            email: "ada@example.com");

        Assert.Null(p);
    }

    [Fact]
    public void TryCreatePrincipal_null_when_supplied_key_mismatches()
    {
        var p = GatewayIdentity.TryCreatePrincipal(
            configuredGatewayKey: Key,
            suppliedGatewayKey: "wrong",
            userId: "user-1",
            displayName: "Ada",
            email: null);

        Assert.Null(p);
    }

    [Fact]
    public void TryCreatePrincipal_null_when_user_id_missing_even_with_valid_key()
    {
        var p = GatewayIdentity.TryCreatePrincipal(
            configuredGatewayKey: Key,
            suppliedGatewayKey: Key,
            userId: "",
            displayName: "Ada",
            email: null);

        Assert.Null(p);
    }

    [Fact]
    public void TryCreatePrincipal_null_when_spoofed_auth_headers_without_key()
    {
        // Client-forged X-Auth-* must not authenticate without proxy key.
        var p = GatewayIdentity.TryCreatePrincipal(
            configuredGatewayKey: Key,
            suppliedGatewayKey: null,
            userId: "attacker",
            displayName: "Attacker",
            email: "a@evil.test");

        Assert.Null(p);
    }

    [Fact]
    public void TryCreatePrincipal_success_with_proxy_key_and_user_claims()
    {
        var p = GatewayIdentity.TryCreatePrincipal(
            configuredGatewayKey: Key,
            suppliedGatewayKey: Key,
            userId: "google-sub-42",
            displayName: "Payton",
            email: "plbyrd@example.com");

        Assert.NotNull(p);
        Assert.True(p!.Identity?.IsAuthenticated);
        Assert.Equal("google-sub-42", p.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Payton", p.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("plbyrd@example.com", p.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal(GatewayAuthHandler.SchemeName, p.Identity?.AuthenticationType);
    }
}

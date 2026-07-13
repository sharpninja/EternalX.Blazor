using System.Security.Claims;
using EternalX.Blazor.Server.Auth;

namespace EternalX.Blazor.Server.Tests.Auth;

public class AdminAccessTests
{
    [Fact]
    public void IsAdmin_true_when_email_matches()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "owner@example.com")
        ], "Gateway"));

        Assert.True(AdminAccess.IsAdmin(user, "owner@example.com"));
    }

    [Fact]
    public void IsAdmin_false_for_other_email_or_anon()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "other@example.com")
        ], "Gateway"));

        Assert.False(AdminAccess.IsAdmin(user, "owner@example.com"));
        Assert.False(AdminAccess.IsAdmin(new ClaimsPrincipal(), "owner@example.com"));
        Assert.False(AdminAccess.IsAdmin(user, null));
    }
}

using System.Security.Claims;

namespace EternalX.Blazor.Server.Auth;

public static class AdminAccess
{
    public static bool IsAdmin(ClaimsPrincipal? user, string? adminEmail)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;
        if (string.IsNullOrWhiteSpace(adminEmail))
            return false;

        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        return !string.IsNullOrWhiteSpace(email) &&
               string.Equals(email, adminEmail.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

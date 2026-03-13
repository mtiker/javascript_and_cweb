using System.Security.Claims;

namespace WebApp.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : Guid.Empty;
    }
}

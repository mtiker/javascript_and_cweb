using System.Security.Claims;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Security;

namespace WebApp.Setup;

public class HttpGymContext(IHttpContextAccessor httpContextAccessor) : IGymContext
{
    public Guid? GymId => TryParseGuid(httpContextAccessor.HttpContext?.User.FindFirstValue(AppClaimTypes.GymId));
    public string? GymCode => httpContextAccessor.HttpContext?.User.FindFirstValue(AppClaimTypes.GymCode);
    public string? ActiveRole => httpContextAccessor.HttpContext?.User.FindFirstValue(AppClaimTypes.ActiveRole);

    public bool IgnoreGymFilter
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return true;
            }

            var path = httpContext.Request.Path;
            if (path.StartsWithSegments("/swagger") || path.StartsWithSegments("/health"))
            {
                return true;
            }

            if (path.StartsWithSegments("/api/v1/system"))
            {
                return true;
            }

            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return true;
            }

            return !GymId.HasValue;
        }
    }

    private static Guid? TryParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

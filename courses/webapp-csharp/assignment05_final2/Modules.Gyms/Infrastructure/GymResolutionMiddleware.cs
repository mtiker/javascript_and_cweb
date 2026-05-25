using SharedKernel.Exceptions;
using App.DAL.EF;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Modules.Gyms.Infrastructure;

/// <summary>
/// Relocated from <c>WebApp/Middleware/</c> in Phase 5. Resolves the gym code
/// segment on tenant routes (<c>api/v{version}/{gymCode}/...</c>) and stashes
/// the resolved id/code on <see cref="HttpContext.Items"/> for downstream
/// services. Still backed by the shared <see cref="AppDbContext"/>; Phase 9
/// will swap this for the Gyms-owned DbContext.
/// </summary>
public sealed class GymResolutionMiddleware(RequestDelegate next)
{
    public const string ResolvedGymIdItemKey = "ResolvedGymId";
    public const string ResolvedGymCodeItemKey = "ResolvedGymCode";

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        var gymCode = context.GetRouteValue("gymCode")?.ToString();
        if (string.IsNullOrWhiteSpace(gymCode))
        {
            await next(context);
            return;
        }

        var gym = await dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Code == gymCode, context.RequestAborted);

        if (gym == null)
        {
            throw new NotFoundException($"Gym '{gymCode}' was not found.");
        }

        if (!gym.IsActive)
        {
            throw new ForbiddenException($"Gym '{gymCode}' is inactive.");
        }

        context.Items[ResolvedGymIdItemKey] = gym.Id;
        context.Items[ResolvedGymCodeItemKey] = gym.Code;

        await next(context);
    }
}

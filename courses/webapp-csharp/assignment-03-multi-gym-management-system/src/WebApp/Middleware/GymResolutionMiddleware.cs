using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;

namespace WebApp.Middleware;

public class GymResolutionMiddleware(RequestDelegate next)
{
    public const string ResolvedGymIdItemKey = "ResolvedGymId";
    public const string ResolvedGymCodeItemKey = "ResolvedGymCode";

    public async Task InvokeAsync(HttpContext context, IAppDbContext dbContext)
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

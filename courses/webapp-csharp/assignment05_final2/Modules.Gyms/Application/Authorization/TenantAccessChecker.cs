using App.BLL.Contracts.Services;
using Modules.Gyms.Application.Persistence;
using SharedKernel.Exceptions;

namespace Modules.Gyms.Application.Authorization;

public class TenantAccessChecker(
    IAuthorizationQueryRepository authorizationQueries,
    ICurrentActorResolver currentActorResolver) : ITenantAccessChecker
{
    public async Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles)
    {
        var context = currentActorResolver.GetCurrent();
        if (!context.IsAuthenticated || !context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            throw new ForbiddenException("An active gym context is required.");
        }

        var gym = await authorizationQueries.FindGymByCodeAsync(gymCode, cancellationToken);
        if (gym == null)
        {
            throw new NotFoundException($"Gym '{gymCode}' was not found.");
        }

        if (context.ActiveGymId != gym.Id || !string.Equals(context.ActiveGymCode, gymCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("The requested gym does not match the active gym context.");
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Any(context.HasRole))
        {
            throw new ForbiddenException("You do not have permission to access this gym resource.");
        }

        return gym.Id;
    }
}

using App.BLL.Contracts.Impersonation;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class ImpersonationService(
    AppDbContext dbContext,
    UserManager<AppUser> userManager)
    : IImpersonationService
{
    public async Task<StartImpersonationResult> StartAsync(Guid actorUserId, StartImpersonationCommand command, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ForbiddenException("Could not resolve actor user id.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length < 8)
        {
            throw new ValidationAppException("Impersonation reason must be at least 8 characters.");
        }

        var actorUser = await userManager.FindByIdAsync(actorUserId.ToString());
        if (actorUser == null)
        {
            throw new ForbiddenException("Actor user was not found.");
        }

        if (!await userManager.IsInRoleAsync(actorUser, RoleNames.SystemAdmin))
        {
            throw new ForbiddenException("Only SystemAdmin can start impersonation.");
        }

        var targetUser = await userManager.FindByEmailAsync(command.TargetUserEmail.Trim());
        if (targetUser == null)
        {
            throw new NotFoundException("Target user was not found.");
        }

        var normalizedSlug = command.CompanySlug.Trim().ToLowerInvariant();
        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Slug == normalizedSlug && entity.IsActive, cancellationToken);

        if (company == null)
        {
            throw new NotFoundException("Target company was not found or is deactivated.");
        }

        var membershipRoles = await dbContext.AppUserRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity =>
                entity.AppUserId == targetUser.Id &&
                entity.CompanyId == company.Id &&
                entity.IsActive)
            .Select(entity => entity.RoleName)
            .ToListAsync(cancellationToken);

        if (membershipRoles.Count == 0)
        {
            throw new ValidationAppException("Target user is not a member of the selected company.");
        }

        var companyRole = ResolveRolePriority(membershipRoles);

        return new StartImpersonationResult(
            targetUser.Id,
            targetUser.Email ?? string.Empty,
            company.Id,
            company.Slug,
            companyRole,
            actorUserId,
            command.Reason.Trim());
    }

    private static string ResolveRolePriority(IReadOnlyCollection<string> roles)
    {
        var priority = new[]
        {
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin,
            RoleNames.CompanyManager,
            RoleNames.CompanyEmployee
        };

        foreach (var role in priority)
        {
            if (roles.Contains(role))
            {
                return role;
            }
        }

        return roles.First();
    }
}

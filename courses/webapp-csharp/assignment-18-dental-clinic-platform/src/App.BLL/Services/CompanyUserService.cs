using App.BLL.Contracts.CompanyUsers;
using App.BLL.Exceptions;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CompanyUserService(
    AppDbContext dbContext,
    ITenantAccessService tenantAccessService,
    ITenantProvider tenantProvider,
    UserManager<AppUser> userManager,
    ISubscriptionPolicyService subscriptionPolicyService)
    : ICompanyUserService
{
    public async Task<IReadOnlyCollection<CompanyUserResult>> ListAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        await EnsureManagementAccessAsync(actorUserId, cancellationToken);

        var users = await dbContext.AppUserRoles
            .AsNoTracking()
            .Join(
                dbContext.Users.AsNoTracking(),
                role => role.AppUserId,
                user => user.Id,
                (role, user) => new CompanyUserResult(
                    role.AppUserId,
                    user.Email ?? user.UserName ?? string.Empty,
                    role.RoleName,
                    role.IsActive,
                    role.AssignedAtUtc))
            .OrderBy(item => item.Email)
            .ThenBy(item => item.RoleName)
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<CompanyUserResult> UpsertAsync(Guid actorUserId, UpsertCompanyUserCommand command, CancellationToken cancellationToken)
    {
        await EnsureManagementAccessAsync(actorUserId, cancellationToken);

        var email = command.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationAppException("User email is required.");
        }

        var normalizedRole = command.RoleName.Trim();
        if (!IsCompanyRole(normalizedRole))
        {
            throw new ValidationAppException("Only company roles can be assigned in tenant user management.");
        }

        var actorIsOwner = await IsOwnerAsync(actorUserId, cancellationToken);
        if (!actorIsOwner && (normalizedRole == RoleNames.CompanyOwner || normalizedRole == RoleNames.CompanyAdmin))
        {
            throw new ForbiddenException("CompanyAdmin can assign only CompanyManager and CompanyEmployee roles.");
        }

        var targetUser = await userManager.FindByEmailAsync(email);
        if (targetUser == null)
        {
            if (string.IsNullOrWhiteSpace(command.TemporaryPassword))
            {
                throw new ValidationAppException("Target user does not exist. Provide temporary password to create a new user.");
            }

            targetUser = new AppUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(targetUser, command.TemporaryPassword);
            if (!createResult.Succeeded)
            {
                var details = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new ValidationAppException($"Could not create user: {details}");
            }
        }

        var companyId = RequireCompanyId();
        var roleLink = await dbContext.AppUserRoles
            .SingleOrDefaultAsync(entity =>
                    entity.AppUserId == targetUser.Id &&
                    entity.CompanyId == companyId &&
                    entity.RoleName == normalizedRole,
                cancellationToken);

        var activatesMembership = command.IsActive && (roleLink == null || !roleLink.IsActive);
        if (activatesMembership)
        {
            var userAlreadyHasActiveMembership = await dbContext.AppUserRoles
                .AsNoTracking()
                .AnyAsync(entity =>
                        entity.AppUserId == targetUser.Id &&
                        entity.CompanyId == companyId &&
                        entity.IsActive &&
                        (roleLink == null || entity.Id != roleLink.Id),
                    cancellationToken);

            if (!userAlreadyHasActiveMembership)
            {
                await subscriptionPolicyService.EnsureCanAddActiveMembershipAsync(cancellationToken);
            }
        }

        if (!command.IsActive && normalizedRole == RoleNames.CompanyOwner)
        {
            await EnsureNotRemovingLastOwnerAsync(targetUser.Id, companyId, roleLink, cancellationToken);
        }

        if (roleLink == null)
        {
            roleLink = new AppUserRole
            {
                AppUserId = targetUser.Id,
                CompanyId = companyId,
                RoleName = normalizedRole,
                IsActive = command.IsActive,
                AssignedAtUtc = DateTime.UtcNow
            };
            dbContext.AppUserRoles.Add(roleLink);
        }
        else
        {
            roleLink.IsActive = command.IsActive;
            roleLink.AssignedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CompanyUserResult(
            roleLink.AppUserId,
            targetUser.Email ?? targetUser.UserName ?? email,
            roleLink.RoleName,
            roleLink.IsActive,
            roleLink.AssignedAtUtc);
    }

    private async Task EnsureNotRemovingLastOwnerAsync(
        Guid targetUserId,
        Guid companyId,
        AppUserRole? currentRoleLink,
        CancellationToken cancellationToken)
    {
        if (currentRoleLink == null || !currentRoleLink.IsActive)
        {
            return;
        }

        var remainingActiveOwners = await dbContext.AppUserRoles
            .AsNoTracking()
            .CountAsync(entity =>
                    entity.CompanyId == companyId &&
                    entity.RoleName == RoleNames.CompanyOwner &&
                    entity.IsActive &&
                    entity.AppUserId != targetUserId,
                cancellationToken);

        if (remainingActiveOwners == 0)
        {
            throw new ValidationAppException("At least one active CompanyOwner must remain in tenant.");
        }
    }

    private async Task EnsureManagementAccessAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        await tenantAccessService.EnsureCompanyRoleAsync(
            actorUserId,
            cancellationToken,
            RoleNames.CompanyOwner,
            RoleNames.CompanyAdmin);
    }

    private async Task<bool> IsOwnerAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        return await dbContext.AppUserRoles
            .AsNoTracking()
            .AnyAsync(entity =>
                    entity.AppUserId == actorUserId &&
                    entity.RoleName == RoleNames.CompanyOwner &&
                    entity.IsActive,
                cancellationToken);
    }

    private Guid RequireCompanyId()
    {
        if (!tenantProvider.CompanyId.HasValue)
        {
            throw new ForbiddenException("Active tenant context is missing.");
        }

        return tenantProvider.CompanyId.Value;
    }

    private static bool IsCompanyRole(string roleName)
    {
        return roleName is RoleNames.CompanyOwner or RoleNames.CompanyAdmin or RoleNames.CompanyManager or RoleNames.CompanyEmployee;
    }
}

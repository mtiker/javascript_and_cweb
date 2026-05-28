using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Services;
using App.Domain.Entities;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Exceptions;

namespace Modules.Users.Application;

/// <summary>
/// Users-module implementation of <see cref="IMemberAccountService"/>. Creates
/// and maintains the <see cref="AppUser"/> login that backs a gym member,
/// keeping the one-login-per-person rule: a returning email reuses the existing
/// account and person and only gains a Member role for the new gym.
/// </summary>
public sealed class MemberAccountService(
    IAppDbContext dbContext,
    UserManager<AppUser> userManager) : IMemberAccountService
{
    public async Task<MemberLoginProvisionResult> ProvisionMemberLoginAsync(
        Guid gymId,
        string email,
        string password,
        MemberPersonDraft demographics,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var existing = await userManager.FindByEmailAsync(normalizedEmail);

        if (existing != null)
        {
            if (!existing.PersonId.HasValue)
            {
                throw new ValidationAppException("This email belongs to an account that is not linked to a person and cannot be enrolled as a member.");
            }

            await EnsureMemberRoleAsync(existing.Id, gymId, cancellationToken);
            return new MemberLoginProvisionResult(existing.PersonId.Value, ReusedExistingAccount: true);
        }

        var person = new Person
        {
            FirstName = demographics.FirstName,
            LastName = demographics.LastName,
            PersonalCode = demographics.PersonalCode,
            DateOfBirth = demographics.DateOfBirth
        };

        var user = new AppUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            DisplayName = $"{demographics.FirstName} {demographics.LastName}".Trim(),
            Person = person
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new ValidationAppException(result.Errors.Select(error => error.Description));
        }

        dbContext.AppUserGymRoles.Add(new AppUserGymRole
        {
            AppUserId = user.Id,
            GymId = gymId,
            RoleName = RoleNames.Member,
            IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MemberLoginProvisionResult(person.Id, ReusedExistingAccount: false);
    }

    public async Task SetPasswordByMemberAsync(string gymCode, Guid memberId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await FindLoginForMemberAsync(gymCode, memberId, cancellationToken)
                   ?? throw new NotFoundException("This member does not have a login account.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            throw new ValidationAppException(result.Errors.Select(error => error.Description));
        }
    }

    public async Task<string?> GetLoginEmailByMemberAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default)
    {
        var user = await FindLoginForMemberAsync(gymCode, memberId, cancellationToken);
        return user?.Email;
    }

    private async Task EnsureMemberRoleAsync(Guid appUserId, Guid gymId, CancellationToken cancellationToken)
    {
        var hasRole = await dbContext.AppUserGymRoles.AnyAsync(
            link => link.AppUserId == appUserId && link.GymId == gymId && link.RoleName == RoleNames.Member,
            cancellationToken);

        if (hasRole)
        {
            return;
        }

        dbContext.AppUserGymRoles.Add(new AppUserGymRole
        {
            AppUserId = appUserId,
            GymId = gymId,
            RoleName = RoleNames.Member,
            IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AppUser?> FindLoginForMemberAsync(string gymCode, Guid memberId, CancellationToken cancellationToken)
    {
        var gymId = await dbContext.Gyms
            .Where(gym => gym.Code == gymCode)
            .Select(gym => (Guid?)gym.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Gym was not found.");

        var personId = await dbContext.Members
            .Where(member => member.Id == memberId && member.GymId == gymId)
            .Select(member => (Guid?)member.PersonId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!personId.HasValue)
        {
            throw new NotFoundException("Member was not found.");
        }

        return await dbContext.Users.FirstOrDefaultAsync(user => user.PersonId == personId.Value, cancellationToken);
    }
}

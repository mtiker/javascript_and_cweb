using System.Globalization;
using App.BLL.Contracts.Services;
using App.Domain.Entities;
using Base.Domain;
using Modules.Gyms.Application.Mappers;
using Modules.Gyms.Application.Persistence;
using Shared.Contracts.Dtos.v1.GymSettings;
using Shared.Contracts.Dtos.v1.GymUsers;
using Shared.Contracts.ModuleApis;
using SharedKernel;
using SharedKernel.Exceptions;

namespace Modules.Gyms.Application;

internal sealed class GymsTenantWorkflowService(
    IGymsTenantPersistenceContext persistenceContext,
    IGymsTenantRepository repository,
    IAuthorizationService authorizationService,
    IUsersModuleApi usersModuleApi,
    IGymsTenantMapper mapper) : IGymsTenantWorkflowService
{
    public async Task<GymSettingsResponse> GetGymSettingsAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member, RoleNames.Trainer, RoleNames.Caretaker);
        var entity = await repository.FindGymSettingsAsync(gymId, cancellationToken)
                     ?? throw new NotFoundException("Gym settings were not found.");
        return mapper.ToGymSettings(entity);
    }

    public async Task<GymSettingsResponse> UpdateGymSettingsAsync(string gymCode, GymSettingsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await repository.FindGymSettingsAsync(gymId, cancellationToken)
                     ?? throw new NotFoundException("Gym settings were not found.");
        entity.CurrencyCode = request.CurrencyCode.Trim();
        entity.TimeZone = request.TimeZone.Trim();
        entity.AllowNonMemberBookings = request.AllowNonMemberBookings;
        entity.BookingCancellationHours = request.BookingCancellationHours;
        entity.PublicDescription = string.IsNullOrWhiteSpace(request.PublicDescription) ? entity.PublicDescription : ToLangStr(request.PublicDescription);
        await persistenceContext.SaveChangesAsync(cancellationToken);
        return mapper.ToGymSettings(entity);
    }

    public async Task<IReadOnlyCollection<GymUserResponse>> GetGymUsersAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var links = await repository.ListGymUsersAsync(gymId, cancellationToken);
        var emails = new Dictionary<Guid, string>();
        foreach (var userId in links.Select(link => link.AppUserId).Distinct())
        {
            var summary = await usersModuleApi.GetUserSummaryAsync(userId, cancellationToken);
            if (summary != null)
            {
                emails[userId] = summary.Email;
            }
        }
        return mapper.ToGymUserList(links, emails);
    }

    public async Task<GymUserResponse> UpsertGymUserAsync(string gymCode, GymUserUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var link = await repository.FindGymUserRoleAsync(gymId, request.AppUserId, request.RoleName, cancellationToken);
        if (link == null)
        {
            link = new AppUserGymRole
            {
                GymId = gymId,
                AppUserId = request.AppUserId,
                RoleName = request.RoleName
            };
            await repository.AddGymUserRoleAsync(link, cancellationToken);
        }

        link.IsActive = request.IsActive;

        await persistenceContext.SaveChangesAsync(cancellationToken);

        var summary = await usersModuleApi.GetUserSummaryAsync(link.AppUserId, cancellationToken)
                      ?? throw new NotFoundException("App user was not found.");

        return mapper.ToGymUser(link, summary.Email);
    }

    public async Task DeleteGymUserAsync(string gymCode, Guid appUserId, string roleName, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var entity = await repository.FindGymUserRoleAsync(gymId, appUserId, roleName, cancellationToken)
                     ?? throw new NotFoundException("Gym user role was not found.");
        repository.RemoveGymUserRole(entity);
        await persistenceContext.SaveChangesAsync(cancellationToken);
    }

    private static LangStr ToLangStr(string value)
    {
        return new LangStr(value.Trim(), CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    }
}

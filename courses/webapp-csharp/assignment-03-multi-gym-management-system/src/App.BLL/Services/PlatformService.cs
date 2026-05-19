using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using App.DTO.v1.System.Platform;
using App.DTO.v1.System;

namespace App.BLL.Services;

public class PlatformService(
    IAppDbContext dbContext,
    UserManager<AppUser> userManager) : IPlatformService
{
    public async Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Gyms
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => new GymSummaryResponse
            {
                GymId = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                IsActive = entity.IsActive,
                City = entity.City,
                Country = entity.Country
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Gyms.AnyAsync(entity => entity.Code == request.Code))
        {
            throw new ValidationAppException("Gym code is already in use.");
        }

        if (await userManager.FindByEmailAsync(request.OwnerEmail) != null)
        {
            throw new ValidationAppException("Owner email is already in use.");
        }

        var gym = new Gym
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToLowerInvariant(),
            RegistrationCode = request.RegistrationCode?.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = request.City.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Country = request.Country.Trim()
        };

        var settings = new GymSettings
        {
            GymId = gym.Id,
            PublicDescription = new LangStr($"{request.Name.Trim()} SaaS workspace", "en")
        };

        var ownerPerson = new Person
        {
            FirstName = request.OwnerFirstName.Trim(),
            LastName = request.OwnerLastName.Trim()
        };

        var ownerUser = new AppUser
        {
            UserName = request.OwnerEmail,
            Email = request.OwnerEmail,
            EmailConfirmed = true,
            DisplayName = $"{request.OwnerFirstName.Trim()} {request.OwnerLastName.Trim()}".Trim(),
            Person = ownerPerson
        };

        var result = await userManager.CreateAsync(ownerUser, request.OwnerPassword);
        if (!result.Succeeded)
        {
            throw new ValidationAppException(result.Errors.Select(error => error.Description));
        }

        var ownerLink = new AppUserGymRole
        {
            AppUserId = ownerUser.Id,
            GymId = gym.Id,
            RoleName = RoleNames.GymOwner,
            IsActive = true
        };

        dbContext.Gyms.Add(gym);
        dbContext.GymSettings.Add(settings);
        dbContext.AppUserGymRoles.Add(ownerLink);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterGymResponse
        {
            GymId = gym.Id,
            GymCode = gym.Code,
            OwnerUserId = ownerUser.Id
        };
    }

    public async Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request, CancellationToken cancellationToken = default)
    {
        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == gymId, cancellationToken)
                  ?? throw new NotFoundException("Gym was not found.");

        gym.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == gymId)
                  ?? throw new NotFoundException("Gym was not found.");

        return new CompanySnapshotResponse
        {
            GymId = gym.Id,
            GymName = gym.Name,
            MemberCount = await dbContext.Members.IgnoreQueryFilters().CountAsync(entity => entity.GymId == gymId && !entity.IsDeleted, cancellationToken),
            SessionCount = await dbContext.TrainingSessions.IgnoreQueryFilters().CountAsync(entity => entity.GymId == gymId && !entity.IsDeleted, cancellationToken),
            OpenMaintenanceTaskCount = await dbContext.MaintenanceTasks.IgnoreQueryFilters()
                .CountAsync(entity => entity.GymId == gymId && !entity.IsDeleted && entity.Status != MaintenanceTaskStatus.Done, cancellationToken)
        };
    }

    public async Task<PlatformAnalyticsResponse> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        return new PlatformAnalyticsResponse
        {
            GymCount = await dbContext.Gyms.CountAsync(cancellationToken),
            UserCount = await dbContext.Users.CountAsync(cancellationToken),
            MemberCount = await dbContext.Members.IgnoreQueryFilters().CountAsync(entity => !entity.IsDeleted, cancellationToken),
            ActiveMaintenanceTaskCount = await dbContext.MaintenanceTasks.IgnoreQueryFilters().CountAsync(
                entity => !entity.IsDeleted && entity.Status != MaintenanceTaskStatus.Done,
                cancellationToken)
        };
    }

}

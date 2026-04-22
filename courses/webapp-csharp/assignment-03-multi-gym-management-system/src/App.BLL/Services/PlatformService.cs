using App.BLL.Contracts.Infrastructure;
using App.BLL.Exceptions;
using App.Domain;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using App.DTO.v1.System.Billing;
using App.DTO.v1.System.Platform;
using App.DTO.v1.System.Support;
using App.DTO.v1.System;

namespace App.BLL.Services;

public class PlatformService(
    IAppDbContext dbContext,
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    IUserContextService userContextService) : IPlatformService
{
    public async Task<IReadOnlyCollection<GymSummaryResponse>> GetGymsAsync()
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
            .ToArrayAsync();
    }

    public async Task<RegisterGymResponse> RegisterGymAsync(RegisterGymRequest request)
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

        var subscription = new Subscription
        {
            GymId = gym.Id,
            Plan = SubscriptionPlan.Starter,
            Status = SubscriptionStatus.Trial,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            MonthlyPrice = 49m,
            CurrencyCode = "EUR"
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
        dbContext.Subscriptions.Add(subscription);
        dbContext.AppUserGymRoles.Add(ownerLink);
        await dbContext.SaveChangesAsync();

        return new RegisterGymResponse
        {
            GymId = gym.Id,
            GymCode = gym.Code,
            OwnerUserId = ownerUser.Id
        };
    }

    public async Task UpdateGymActivationAsync(Guid gymId, UpdateGymActivationRequest request)
    {
        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == gymId)
                  ?? throw new NotFoundException("Gym was not found.");

        gym.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<SubscriptionSummaryResponse>> GetSubscriptionsAsync()
    {
        return await dbContext.Subscriptions
            .AsNoTracking()
            .Include(subscription => subscription.Gym)
            .OrderBy(subscription => subscription.Gym!.Name)
            .Select(subscription => new SubscriptionSummaryResponse
            {
                GymId = subscription.GymId,
                GymName = subscription.Gym!.Name,
                Plan = subscription.Plan,
                Status = subscription.Status,
                MonthlyPrice = subscription.MonthlyPrice,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate
            })
            .ToArrayAsync();
    }

    public async Task<SubscriptionSummaryResponse> UpdateSubscriptionAsync(Guid gymId, UpdateSubscriptionRequest request)
    {
        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == gymId)
                  ?? throw new NotFoundException("Gym was not found.");

        var subscription = await dbContext.Subscriptions.FirstOrDefaultAsync(entity => entity.GymId == gymId);
        if (subscription == null)
        {
            subscription = new Subscription
            {
                GymId = gymId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date)
            };
            dbContext.Subscriptions.Add(subscription);
        }

        subscription.Plan = request.Plan;
        subscription.Status = request.Status;
        subscription.EndDate = request.EndDate;
        subscription.MonthlyPrice = request.MonthlyPrice;

        await dbContext.SaveChangesAsync();

        return new SubscriptionSummaryResponse
        {
            GymId = gymId,
            GymName = gym.Name,
            Plan = subscription.Plan,
            Status = subscription.Status,
            MonthlyPrice = subscription.MonthlyPrice,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate
        };
    }

    public async Task<IReadOnlyCollection<SupportTicketResponse>> GetSupportTicketsAsync()
    {
        return await dbContext.SupportTickets
            .AsNoTracking()
            .Include(ticket => ticket.Gym)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(ticket => new SupportTicketResponse
            {
                TicketId = ticket.Id,
                GymId = ticket.GymId,
                GymName = ticket.Gym!.Name,
                Title = ticket.Title,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedAtUtc = ticket.CreatedAtUtc
            })
            .ToArrayAsync();
    }

    public async Task<SupportTicketResponse> CreateSupportTicketAsync(Guid gymId, SupportTicketRequest request)
    {
        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == gymId)
                  ?? throw new NotFoundException("Gym was not found.");

        var ticket = new SupportTicket
        {
            GymId = gymId,
            CreatedByUserId = userContextService.GetCurrent().UserId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            Status = SupportTicketStatus.Open
        };

        dbContext.SupportTickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        return new SupportTicketResponse
        {
            TicketId = ticket.Id,
            GymId = gymId,
            GymName = gym.Name,
            Title = ticket.Title,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedAtUtc = ticket.CreatedAtUtc
        };
    }

    public async Task<CompanySnapshotResponse> GetGymSnapshotAsync(Guid gymId)
    {
        var gym = await dbContext.Gyms.FirstOrDefaultAsync(entity => entity.Id == gymId)
                  ?? throw new NotFoundException("Gym was not found.");

        return new CompanySnapshotResponse
        {
            GymId = gym.Id,
            GymName = gym.Name,
            MemberCount = await dbContext.Members.IgnoreQueryFilters().CountAsync(entity => entity.GymId == gymId && !entity.IsDeleted),
            SessionCount = await dbContext.TrainingSessions.IgnoreQueryFilters().CountAsync(entity => entity.GymId == gymId && !entity.IsDeleted),
            OpenMaintenanceTaskCount = await dbContext.MaintenanceTasks.IgnoreQueryFilters()
                .CountAsync(entity => entity.GymId == gymId && !entity.IsDeleted && entity.Status != MaintenanceTaskStatus.Done)
        };
    }

    public async Task<PlatformAnalyticsResponse> GetAnalyticsAsync()
    {
        return new PlatformAnalyticsResponse
        {
            GymCount = await dbContext.Gyms.CountAsync(),
            UserCount = await dbContext.Users.CountAsync(),
            MemberCount = await dbContext.Members.IgnoreQueryFilters().CountAsync(entity => !entity.IsDeleted),
            OpenSupportTicketCount = await dbContext.SupportTickets.CountAsync(entity => entity.Status != SupportTicketStatus.Resolved)
        };
    }

    public async Task<StartImpersonationResponse> StartImpersonationAsync(StartImpersonationRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
                   ?? throw new NotFoundException("User was not found.");

        var systemRoles = (await userManager.GetRolesAsync(user))
            .Where(RoleNames.SystemRoles.Contains)
            .ToArray();

        AppUserGymRole? activeLink;

        if (!string.IsNullOrWhiteSpace(request.GymCode))
        {
            activeLink = await dbContext.AppUserGymRoles
                .Include(link => link.Gym)
                .FirstOrDefaultAsync(link =>
                    link.AppUserId == user.Id &&
                    link.Gym!.Code == request.GymCode &&
                    link.IsActive);
        }
        else
        {
            activeLink = await dbContext.AppUserGymRoles
                .Include(link => link.Gym)
                .Where(link => link.AppUserId == user.Id && link.IsActive)
                .OrderBy(link => link.Gym!.Name)
                .ThenBy(link => link.RoleName)
                .FirstOrDefaultAsync();
        }

        return new StartImpersonationResponse
        {
            Jwt = tokenService.CreateJwt(user, systemRoles, activeLink),
            UserId = user.Id,
            GymCode = activeLink?.Gym?.Code
        };
    }
}

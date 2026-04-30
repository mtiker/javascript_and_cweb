using System.Globalization;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Admin.Services;

public interface IAdminDashboardPageService
{
    Task<AdminDashboardViewModel> BuildAsync(CancellationToken cancellationToken = default);
}

public interface IAdminGymsPageService
{
    Task<AdminGymsPageViewModel> BuildAsync(CancellationToken cancellationToken = default);
}

public interface IAdminOperationsPageService
{
    Task<AdminOperationsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default);
}

public interface IAdminSessionsPageService
{
    Task<AdminSessionsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default);
}

public sealed class AdminDashboardPageService(
    IUserContextService userContextService,
    IPlatformService platformService) : IAdminDashboardPageService
{
    public async Task<AdminDashboardViewModel> BuildAsync(CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        var analytics = await platformService.GetAnalyticsAsync(cancellationToken);
        var viewModel = new AdminDashboardViewModel
        {
            ActiveGymCode = context.ActiveGymCode,
            ActiveRole = context.ActiveRole,
            SystemRoles = context.SystemRoles,
            GymCount = analytics.GymCount
        };

        if (context.ActiveGymId.HasValue)
        {
            var snapshot = await platformService.GetGymSnapshotAsync(context.ActiveGymId.Value, cancellationToken);
            viewModel.MemberCount = snapshot.MemberCount;
            viewModel.SessionCount = snapshot.SessionCount;
            viewModel.OpenMaintenanceTaskCount = snapshot.OpenMaintenanceTaskCount;
        }

        return viewModel;
    }
}

public sealed class AdminGymsPageService(IPlatformService platformService) : IAdminGymsPageService
{
    public async Task<AdminGymsPageViewModel> BuildAsync(CancellationToken cancellationToken = default)
    {
        var gyms = (await platformService.GetGymsAsync(cancellationToken))
            .Select(gym => new AdminGymSummaryViewModel
            {
                Name = gym.Name,
                Code = gym.Code,
                City = gym.City,
                IsActive = gym.IsActive
            })
            .ToArray();

        return new AdminGymsPageViewModel
        {
            Gyms = gyms
        };
    }
}

public sealed class AdminOperationsPageService(AppDbContext dbContext) : IAdminOperationsPageService
{
    public async Task<AdminOperationsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var openingHours = await dbContext.OpeningHours
            .AsNoTracking()
            .Where(entity => entity.GymId == gymId)
            .OrderBy(entity => entity.Weekday)
            .Select(entity => new OpeningHoursSummaryViewModel
            {
                Weekday = entity.Weekday,
                OpensAt = entity.OpensAt,
                ClosesAt = entity.ClosesAt
            })
            .ToArrayAsync(cancellationToken);

        var equipment = (await dbContext.Equipment
                .AsNoTracking()
                .Include(entity => entity.EquipmentModel)
                .Where(entity => entity.GymId == gymId)
                .OrderBy(entity => entity.AssetTag)
                .Take(20)
                .ToArrayAsync(cancellationToken))
            .Select(entity => new EquipmentSummaryViewModel
            {
                AssetTag = entity.AssetTag ?? entity.SerialNumber ?? entity.Id.ToString(),
                ModelName = entity.EquipmentModel?.Name.Translate(culture) ?? string.Empty,
                Status = entity.CurrentStatus
            })
            .ToArray();

        var maintenanceTasks = (await dbContext.MaintenanceTasks
                .AsNoTracking()
                .Include(entity => entity.Equipment)
                .Include(entity => entity.AssignedStaff)
                    .ThenInclude(entity => entity!.Person)
                .Where(entity => entity.GymId == gymId && entity.Status != MaintenanceTaskStatus.Done)
                .OrderBy(entity => entity.DueAtUtc)
                .Take(20)
                .ToArrayAsync(cancellationToken))
            .Select(entity => new MaintenanceSummaryViewModel
            {
                AssetTag = entity.Equipment?.AssetTag ?? entity.Equipment?.SerialNumber ?? entity.EquipmentId.ToString(),
                TaskType = entity.TaskType,
                Status = entity.Status,
                AssignedTo = entity.AssignedStaff == null
                    ? null
                    : $"{entity.AssignedStaff.Person?.FirstName} {entity.AssignedStaff.Person?.LastName}".Trim(),
                DueAtUtc = entity.DueAtUtc
            })
            .ToArray();

        return new AdminOperationsPageViewModel
        {
            GymCode = gymCode,
            OpeningHours = openingHours,
            Equipment = equipment,
            MaintenanceTasks = maintenanceTasks
        };
    }
}

public sealed class AdminSessionsPageService(AppDbContext dbContext) : IAdminSessionsPageService
{
    public async Task<AdminSessionsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var sessions = (await dbContext.TrainingSessions
                .AsNoTracking()
                .Include(entity => entity.Bookings)
                .Include(entity => entity.WorkShifts)
                    .ThenInclude(entity => entity.Contract)
                        .ThenInclude(entity => entity!.Staff)
                            .ThenInclude(entity => entity!.Person)
                .Where(entity => entity.GymId == gymId)
                .OrderBy(entity => entity.StartAtUtc)
                .Take(20)
                .ToArrayAsync(cancellationToken))
            .Select(entity => new AdminSessionSummaryViewModel
            {
                Name = entity.Name.Translate(culture) ?? entity.Name.ToString(),
                StartAtUtc = entity.StartAtUtc,
                EndAtUtc = entity.EndAtUtc,
                Capacity = entity.Capacity,
                BookingCount = entity.Bookings.Count(booking => booking.Status != BookingStatus.Cancelled),
                Status = entity.Status,
                TrainerNames = string.Join(", ", entity.WorkShifts
                    .Where(shift => shift.ShiftType == ShiftType.Training)
                    .Select(shift => $"{shift.Contract?.Staff?.Person?.FirstName} {shift.Contract?.Staff?.Person?.LastName}".Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
            })
            .ToArray();

        return new AdminSessionsPageViewModel
        {
            GymCode = gymCode,
            Sessions = sessions
        };
    }
}

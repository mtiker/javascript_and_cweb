using App.BLL.Infrastructure;
using App.BLL.Contracts.Services;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1.MaintenanceTasks;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Areas.Client.Services;

public interface IClientMaintenancePageService
{
    Task<MaintenancePageViewModel?> BuildIndexAsync(CancellationToken cancellationToken = default);

    Task<MaintenanceTaskDetailPageViewModel?> BuildDetailsAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(Guid taskId, MaintenanceTaskStatus status, string? notes, CancellationToken cancellationToken = default);
}

public sealed class ClientMaintenancePageService(
    IAppDbContext dbContext,
    IUserContextService userContextService,
    App.BLL.Contracts.Services.IAuthorizationService authorizationService,
    IMaintenanceWorkflowService maintenanceWorkflowService) : IClientMaintenancePageService
{
    public async Task<MaintenancePageViewModel?> BuildIndexAsync(CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return null;
        }

        var staff = await authorizationService.GetCurrentStaffAsync(context.ActiveGymId.Value, cancellationToken);
        var tasks = Array.Empty<MaintenanceTaskResponse>();

        if (staff != null &&
            (context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin) || context.HasRole(RoleNames.Caretaker)))
        {
            tasks = (await maintenanceWorkflowService.GetMaintenanceTasksAsync(context.ActiveGymCode, cancellationToken: cancellationToken))
                .Where(entity => entity.AssignedStaffId == staff.Id)
                .OrderBy(entity => entity.DueAtUtc)
                .ToArray();
        }

        return new MaintenancePageViewModel
        {
            GymCode = context.ActiveGymCode,
            Tasks = tasks
        };
    }

    public async Task<MaintenanceTaskDetailPageViewModel?> BuildDetailsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!context.ActiveGymId.HasValue || string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return null;
        }

        var task = (await maintenanceWorkflowService.GetMaintenanceTasksAsync(context.ActiveGymCode, cancellationToken: cancellationToken))
            .FirstOrDefault(entity => entity.Id == taskId);
        if (task == null)
        {
            return null;
        }

        var equipmentLabel = await dbContext.Equipment
            .Where(entity => entity.Id == task.EquipmentId)
            .Select(entity => entity.AssetTag ?? entity.SerialNumber ?? entity.Id.ToString())
            .FirstOrDefaultAsync(cancellationToken) ?? task.EquipmentId.ToString();

        return new MaintenanceTaskDetailPageViewModel
        {
            GymCode = context.ActiveGymCode,
            Task = task,
            EquipmentLabel = equipmentLabel,
            NextStatus = task.Status == MaintenanceTaskStatus.Open
                ? MaintenanceTaskStatus.InProgress
                : MaintenanceTaskStatus.Done,
            Notes = task.Notes
        };
    }

    public async Task<bool> UpdateStatusAsync(
        Guid taskId,
        MaintenanceTaskStatus status,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return false;
        }

        await maintenanceWorkflowService.UpdateTaskStatusAsync(context.ActiveGymCode, taskId, new MaintenanceStatusUpdateRequest
        {
            Status = status,
            Notes = notes
        }, cancellationToken);

        return true;
    }
}

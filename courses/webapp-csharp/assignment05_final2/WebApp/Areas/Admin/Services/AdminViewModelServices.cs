using System.Globalization;
using SharedKernel.Exceptions;
using App.DAL.Contracts.Persistence;
using App.BLL.Contracts.Services;
using App.BLL.Contracts.Services.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Equipment;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;
using Shared.Contracts.Dtos.v1.Members;
using Shared.Contracts.Dtos.v1.MembershipPackages;
using Shared.Contracts.Dtos.v1.Memberships;
using Shared.Contracts.Dtos.v1.Staff;
using Shared.Contracts.Dtos.v1.TrainingCategories;
using Shared.Contracts.Dtos.v1.TrainingSessions;
using Shared.Contracts.Dtos.v1.System;
using Shared.Contracts.Dtos.v1.System.Platform;
using Modules.Memberships.Application.Persistence;
using Modules.Training.Application.Persistence;
using WebApp.Models;

namespace WebApp.Areas.Admin.Services;

public interface IAdminDashboardPageService
{
    Task<AdminDashboardViewModel> BuildAsync(CancellationToken cancellationToken = default);
}

public enum AdminGymOperationStatus
{
    Success,
    NotFound,
    ValidationFailed
}

public sealed record AdminGymOperationResult(
    AdminGymOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminGymOperationResult Success { get; } =
        new(AdminGymOperationStatus.Success, Array.Empty<string>());

    public static AdminGymOperationResult NotFound { get; } =
        new(AdminGymOperationStatus.NotFound, Array.Empty<string>());

    public static AdminGymOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminGymOperationStatus.ValidationFailed, errors.ToArray());
}

public interface IAdminGymsPageService
{
    Task<AdminGymsPageViewModel> BuildAsync(CancellationToken cancellationToken = default);
    Task<AdminGymFormViewModel?> GetEditFormAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<AdminGymOperationResult> CreateAsync(AdminGymFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminGymOperationResult> UpdateAsync(Guid gymId, AdminGymFormViewModel form, CancellationToken cancellationToken = default);
}

public enum AdminEquipmentOperationStatus
{
    Success,
    NotFound,
    ValidationFailed
}

public sealed record AdminEquipmentOperationResult(
    AdminEquipmentOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminEquipmentOperationResult Success { get; } =
        new(AdminEquipmentOperationStatus.Success, Array.Empty<string>());

    public static AdminEquipmentOperationResult NotFound { get; } =
        new(AdminEquipmentOperationStatus.NotFound, Array.Empty<string>());

    public static AdminEquipmentOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminEquipmentOperationStatus.ValidationFailed, errors.ToArray());
}

public interface IAdminOperationsPageService
{
    Task<AdminOperationsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default);
    Task<AdminEquipmentFormViewModel> BuildEquipmentCreateFormAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<AdminEquipmentFormViewModel?> GetEquipmentEditFormAsync(string gymCode, Guid equipmentId, CancellationToken cancellationToken = default);
    Task<AdminEquipmentDeleteViewModel?> GetEquipmentDeleteViewAsync(string gymCode, Guid equipmentId, CancellationToken cancellationToken = default);
    Task<AdminEquipmentOperationResult> CreateEquipmentAsync(string gymCode, AdminEquipmentFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminEquipmentOperationResult> UpdateEquipmentAsync(string gymCode, Guid equipmentId, AdminEquipmentFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminEquipmentOperationResult> DeleteEquipmentAsync(string gymCode, Guid equipmentId, CancellationToken cancellationToken = default);
    Task PopulateEquipmentOptionsAsync(string gymCode, AdminEquipmentFormViewModel form, CancellationToken cancellationToken = default);

    Task<AdminMaintenanceTaskFormViewModel> BuildTaskCreateFormAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<AdminMaintenanceTaskEditFormViewModel?> GetTaskEditFormAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default);
    Task<AdminMaintenanceTaskDeleteViewModel?> GetTaskDeleteViewAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default);
    Task<AdminEquipmentOperationResult> CreateTaskAsync(string gymCode, AdminMaintenanceTaskFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminEquipmentOperationResult> UpdateTaskAsync(string gymCode, Guid taskId, AdminMaintenanceTaskEditFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminEquipmentOperationResult> DeleteTaskAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default);
    Task PopulateTaskCreateOptionsAsync(string gymCode, AdminMaintenanceTaskFormViewModel form, CancellationToken cancellationToken = default);
    Task PopulateTaskEditOptionsAsync(string gymCode, AdminMaintenanceTaskEditFormViewModel form, CancellationToken cancellationToken = default);
}

public enum AdminSessionOperationStatus
{
    Success,
    NotFound,
    ValidationFailed
}

public sealed record AdminSessionOperationResult(
    AdminSessionOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminSessionOperationResult Success { get; } =
        new(AdminSessionOperationStatus.Success, Array.Empty<string>());

    public static AdminSessionOperationResult NotFound { get; } =
        new(AdminSessionOperationStatus.NotFound, Array.Empty<string>());

    public static AdminSessionOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminSessionOperationStatus.ValidationFailed, errors.ToArray());
}

public interface IAdminSessionsPageService
{
    Task<AdminSessionsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default);
    Task<AdminSessionFormViewModel> BuildCreateFormAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<AdminSessionFormViewModel?> GetEditFormAsync(string gymCode, Guid sessionId, CancellationToken cancellationToken = default);
    Task<AdminSessionDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid sessionId, CancellationToken cancellationToken = default);
    Task<AdminSessionOperationResult> CreateAsync(string gymCode, AdminSessionFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminSessionOperationResult> UpdateAsync(string gymCode, Guid sessionId, AdminSessionFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminSessionOperationResult> DeleteAsync(string gymCode, Guid sessionId, CancellationToken cancellationToken = default);
    Task PopulateOptionsAsync(string gymCode, AdminSessionFormViewModel form, CancellationToken cancellationToken = default);
}

public enum AdminMembershipOperationStatus
{
    Success,
    NotFound,
    ValidationFailed
}

public sealed record AdminMembershipOperationResult(
    AdminMembershipOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminMembershipOperationResult Success { get; } =
        new(AdminMembershipOperationStatus.Success, Array.Empty<string>());

    public static AdminMembershipOperationResult NotFound { get; } =
        new(AdminMembershipOperationStatus.NotFound, Array.Empty<string>());

    public static AdminMembershipOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminMembershipOperationStatus.ValidationFailed, errors.ToArray());
}

public interface IAdminMembershipsPageService
{
    Task<AdminMembershipsPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<AdminMembershipSellFormViewModel> BuildSellFormAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<AdminMembershipEditFormViewModel?> GetEditFormAsync(string gymCode, Guid membershipId, CancellationToken cancellationToken = default);
    Task<AdminMembershipDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid membershipId, CancellationToken cancellationToken = default);
    Task<AdminMembershipOperationResult> SellAsync(string gymCode, AdminMembershipSellFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminMembershipOperationResult> UpdateAsync(string gymCode, Guid membershipId, AdminMembershipEditFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminMembershipOperationResult> DeleteAsync(string gymCode, Guid membershipId, CancellationToken cancellationToken = default);
    Task PopulateSellOptionsAsync(string gymCode, AdminMembershipSellFormViewModel form, CancellationToken cancellationToken = default);
    Task PopulateEditOptionsAsync(string gymCode, AdminMembershipEditFormViewModel form, CancellationToken cancellationToken = default);
}

public enum AdminMemberOperationStatus
{
    Success,
    NotFound,
    ValidationFailed
}

public sealed record AdminMemberOperationResult(
    AdminMemberOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminMemberOperationResult Success { get; } =
        new(AdminMemberOperationStatus.Success, Array.Empty<string>());

    public static AdminMemberOperationResult NotFound { get; } =
        new(AdminMemberOperationStatus.NotFound, Array.Empty<string>());

    public static AdminMemberOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminMemberOperationStatus.ValidationFailed, errors.ToArray());
}

public interface IAdminMembersPageService
{
    Task<AdminMembersPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default);

    Task<AdminMemberFormViewModel?> GetEditFormAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default);

    Task<AdminMemberDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default);

    Task<AdminMemberOperationResult> CreateAsync(string gymCode, AdminMemberFormViewModel form, CancellationToken cancellationToken = default);

    Task<AdminMemberOperationResult> UpdateAsync(string gymCode, Guid memberId, AdminMemberFormViewModel form, CancellationToken cancellationToken = default);

    Task<AdminMemberOperationResult> DeleteAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default);
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
                Id = gym.GymId,
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

    public async Task<AdminGymFormViewModel?> GetEditFormAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = (await platformService.GetGymsAsync(cancellationToken))
            .FirstOrDefault(item => item.GymId == gymId);
        if (gym is null)
        {
            return null;
        }

        return new AdminGymFormViewModel
        {
            Id = gym.GymId,
            Name = gym.Name,
            Code = gym.Code,
            RegistrationCode = gym.RegistrationCode,
            AddressLine = gym.AddressLine,
            City = gym.City,
            PostalCode = gym.PostalCode,
            Country = gym.Country,
            IsActive = gym.IsActive
        };
    }

    public async Task<AdminGymOperationResult> CreateAsync(AdminGymFormViewModel form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.OwnerEmail) ||
            string.IsNullOrWhiteSpace(form.OwnerPassword) ||
            string.IsNullOrWhiteSpace(form.OwnerFirstName) ||
            string.IsNullOrWhiteSpace(form.OwnerLastName))
        {
            return AdminGymOperationResult.ValidationFailed(
                ["Owner email, password, first name and last name are required when registering a gym."]);
        }

        try
        {
            await platformService.RegisterGymAsync(new RegisterGymRequest
            {
                Name = form.Name.Trim(),
                Code = form.Code.Trim(),
                RegistrationCode = string.IsNullOrWhiteSpace(form.RegistrationCode) ? null : form.RegistrationCode.Trim(),
                AddressLine = form.AddressLine.Trim(),
                City = form.City.Trim(),
                PostalCode = form.PostalCode.Trim(),
                Country = form.Country.Trim(),
                OwnerEmail = form.OwnerEmail.Trim(),
                OwnerPassword = form.OwnerPassword,
                OwnerFirstName = form.OwnerFirstName.Trim(),
                OwnerLastName = form.OwnerLastName.Trim()
            }, cancellationToken);
            return AdminGymOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminGymOperationResult.ValidationFailed(exception.Errors);
        }
    }

    public async Task<AdminGymOperationResult> UpdateAsync(Guid gymId, AdminGymFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await platformService.UpdateGymProfileAsync(gymId, new UpdateGymProfileRequest
            {
                Name = form.Name.Trim(),
                RegistrationCode = string.IsNullOrWhiteSpace(form.RegistrationCode) ? null : form.RegistrationCode.Trim(),
                AddressLine = form.AddressLine.Trim(),
                City = form.City.Trim(),
                PostalCode = form.PostalCode.Trim(),
                Country = form.Country.Trim(),
                IsActive = form.IsActive
            }, cancellationToken);
            return AdminGymOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminGymOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminGymOperationResult.NotFound;
        }
    }
}

public sealed class AdminOperationsPageService(
    IAdminOperationsQueryService operationsQueryService,
    IMaintenanceWorkflowService maintenanceWorkflowService,
    IStaffWorkflowService staffWorkflowService) : IAdminOperationsPageService
{
    public async Task<AdminOperationsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var snapshot = await operationsQueryService.GetSnapshotAsync(gymId, cancellationToken);

        return new AdminOperationsPageViewModel
        {
            GymCode = gymCode,
            Equipment = snapshot.Equipment
                .Select(row => new EquipmentSummaryViewModel
                {
                    Id = row.Id,
                    AssetTag = row.AssetTag,
                    ModelName = row.ModelName?.Translate(culture) ?? string.Empty,
                    Status = row.Status
                })
                .ToArray(),
            MaintenanceTasks = snapshot.MaintenanceTasks
                .Select(row => new MaintenanceSummaryViewModel
                {
                    Id = row.Id,
                    AssetTag = row.AssetTag,
                    TaskType = row.TaskType,
                    Status = row.Status,
                    AssignedTo = row.AssignedTo,
                    DueAtUtc = row.DueAtUtc
                })
                .ToArray()
        };
    }

    public async Task<AdminEquipmentFormViewModel> BuildEquipmentCreateFormAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var form = new AdminEquipmentFormViewModel
        {
            GymCode = gymCode,
            CurrentStatus = EquipmentStatus.Active
        };

        await PopulateEquipmentOptionsAsync(gymCode, form, cancellationToken);
        return form;
    }

    public async Task<AdminEquipmentFormViewModel?> GetEquipmentEditFormAsync(string gymCode, Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var equipment = (await maintenanceWorkflowService.GetEquipmentAsync(gymCode, cancellationToken: cancellationToken))
            .FirstOrDefault(item => item.Id == equipmentId);
        if (equipment is null)
        {
            return null;
        }

        var form = new AdminEquipmentFormViewModel
        {
            Id = equipment.Id,
            GymCode = gymCode,
            EquipmentModelId = equipment.EquipmentModelId,
            AssetTag = equipment.AssetTag,
            SerialNumber = equipment.SerialNumber,
            CurrentStatus = equipment.CurrentStatus,
            CommissionedAt = equipment.CommissionedAt,
            DecommissionedAt = equipment.DecommissionedAt,
            Notes = equipment.Notes
        };

        await PopulateEquipmentOptionsAsync(gymCode, form, cancellationToken);
        return form;
    }

    public async Task<AdminEquipmentDeleteViewModel?> GetEquipmentDeleteViewAsync(string gymCode, Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var equipment = (await maintenanceWorkflowService.GetEquipmentAsync(gymCode, cancellationToken: cancellationToken))
            .FirstOrDefault(item => item.Id == equipmentId);
        if (equipment is null)
        {
            return null;
        }

        var models = await maintenanceWorkflowService.GetEquipmentModelsAsync(gymCode, cancellationToken);
        var modelName = models.FirstOrDefault(model => model.Id == equipment.EquipmentModelId)?.Name ?? string.Empty;

        return new AdminEquipmentDeleteViewModel
        {
            Id = equipment.Id,
            GymCode = gymCode,
            AssetTag = equipment.AssetTag ?? equipment.SerialNumber ?? equipment.Id.ToString(),
            ModelName = modelName,
            Status = equipment.CurrentStatus
        };
    }

    public async Task<AdminEquipmentOperationResult> CreateEquipmentAsync(string gymCode, AdminEquipmentFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await maintenanceWorkflowService.CreateEquipmentAsync(gymCode, ToUpsertRequest(form), cancellationToken);
            return AdminEquipmentOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminEquipmentOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminEquipmentOperationResult.NotFound;
        }
    }

    public async Task<AdminEquipmentOperationResult> UpdateEquipmentAsync(string gymCode, Guid equipmentId, AdminEquipmentFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await maintenanceWorkflowService.UpdateEquipmentAsync(gymCode, equipmentId, ToUpsertRequest(form), cancellationToken);
            return AdminEquipmentOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminEquipmentOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminEquipmentOperationResult.NotFound;
        }
    }

    public async Task<AdminEquipmentOperationResult> DeleteEquipmentAsync(string gymCode, Guid equipmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await maintenanceWorkflowService.DeleteEquipmentAsync(gymCode, equipmentId, cancellationToken);
            return AdminEquipmentOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminEquipmentOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminEquipmentOperationResult.NotFound;
        }
    }

    public async Task PopulateEquipmentOptionsAsync(string gymCode, AdminEquipmentFormViewModel form, CancellationToken cancellationToken = default)
    {
        var models = await maintenanceWorkflowService.GetEquipmentModelsAsync(gymCode, cancellationToken);
        form.EquipmentModelOptions = models
            .Select(model => new SelectListItem(
                model.Manufacturer is null ? model.Name : $"{model.Name} ({model.Manufacturer})",
                model.Id.ToString(),
                model.Id == form.EquipmentModelId))
            .ToArray();
    }

    private static EquipmentUpsertRequest ToUpsertRequest(AdminEquipmentFormViewModel form) =>
        new()
        {
            EquipmentModelId = form.EquipmentModelId,
            AssetTag = string.IsNullOrWhiteSpace(form.AssetTag) ? null : form.AssetTag.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(form.SerialNumber) ? null : form.SerialNumber.Trim(),
            CurrentStatus = form.CurrentStatus,
            CommissionedAt = form.CommissionedAt,
            DecommissionedAt = form.DecommissionedAt,
            Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
        };

    public async Task<AdminMaintenanceTaskFormViewModel> BuildTaskCreateFormAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var form = new AdminMaintenanceTaskFormViewModel
        {
            GymCode = gymCode,
            TaskType = MaintenanceTaskType.Scheduled,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceTaskStatus.Open
        };

        await PopulateTaskCreateOptionsAsync(gymCode, form, cancellationToken);
        return form;
    }

    public async Task<AdminMaintenanceTaskEditFormViewModel?> GetTaskEditFormAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await FindTaskAsync(gymCode, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var form = new AdminMaintenanceTaskEditFormViewModel
        {
            Id = task.Id,
            GymCode = gymCode,
            EquipmentName = string.IsNullOrWhiteSpace(task.EquipmentAssetTag) ? task.EquipmentName : $"{task.EquipmentName} ({task.EquipmentAssetTag})",
            TaskType = task.TaskType,
            Priority = task.Priority,
            Status = task.Status,
            AssignedStaffId = task.AssignedStaffId,
            Notes = task.Notes
        };

        await PopulateTaskEditOptionsAsync(gymCode, form, cancellationToken);
        return form;
    }

    public async Task<AdminMaintenanceTaskDeleteViewModel?> GetTaskDeleteViewAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await FindTaskAsync(gymCode, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        return new AdminMaintenanceTaskDeleteViewModel
        {
            Id = task.Id,
            GymCode = gymCode,
            EquipmentName = string.IsNullOrWhiteSpace(task.EquipmentAssetTag) ? task.EquipmentName : $"{task.EquipmentName} ({task.EquipmentAssetTag})",
            TaskType = task.TaskType,
            Status = task.Status,
            DueAtUtc = task.DueAtUtc
        };
    }

    public async Task<AdminEquipmentOperationResult> CreateTaskAsync(string gymCode, AdminMaintenanceTaskFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await maintenanceWorkflowService.CreateTaskAsync(gymCode, new MaintenanceTaskUpsertRequest
            {
                EquipmentId = form.EquipmentId,
                AssignedStaffId = form.AssignedStaffId,
                TaskType = form.TaskType,
                Priority = form.Priority,
                Status = form.Status,
                DueAtUtc = form.DueAt.HasValue ? ToUtc(form.DueAt.Value) : null,
                Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
            }, cancellationToken);
            return AdminEquipmentOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminEquipmentOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminEquipmentOperationResult.NotFound;
        }
    }

    public async Task<AdminEquipmentOperationResult> UpdateTaskAsync(string gymCode, Guid taskId, AdminMaintenanceTaskEditFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            var current = await FindTaskAsync(gymCode, taskId, cancellationToken);
            if (current is null)
            {
                return AdminEquipmentOperationResult.NotFound;
            }

            if (current.AssignedStaffId != form.AssignedStaffId)
            {
                await maintenanceWorkflowService.UpdateTaskAssignmentAsync(gymCode, taskId, new MaintenanceAssignmentUpdateRequest
                {
                    AssignedStaffId = form.AssignedStaffId,
                    Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
                }, cancellationToken);
            }

            if (current.Status != form.Status)
            {
                await maintenanceWorkflowService.UpdateTaskStatusAsync(gymCode, taskId, new MaintenanceStatusUpdateRequest
                {
                    Status = form.Status,
                    Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
                }, cancellationToken);
            }

            return AdminEquipmentOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminEquipmentOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminEquipmentOperationResult.NotFound;
        }
    }

    public async Task<AdminEquipmentOperationResult> DeleteTaskAsync(string gymCode, Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            await maintenanceWorkflowService.DeleteMaintenanceTaskAsync(gymCode, taskId, cancellationToken);
            return AdminEquipmentOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminEquipmentOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminEquipmentOperationResult.NotFound;
        }
    }

    public async Task PopulateTaskCreateOptionsAsync(string gymCode, AdminMaintenanceTaskFormViewModel form, CancellationToken cancellationToken = default)
    {
        var equipment = await maintenanceWorkflowService.GetEquipmentAsync(gymCode, cancellationToken: cancellationToken);
        form.EquipmentOptions = equipment
            .Select(item => new SelectListItem(
                item.AssetTag ?? item.SerialNumber ?? item.Id.ToString(),
                item.Id.ToString(),
                item.Id == form.EquipmentId))
            .ToArray();

        form.StaffOptions = await BuildStaffOptionsAsync(gymCode, form.AssignedStaffId, cancellationToken);
    }

    public async Task PopulateTaskEditOptionsAsync(string gymCode, AdminMaintenanceTaskEditFormViewModel form, CancellationToken cancellationToken = default)
    {
        form.StaffOptions = await BuildStaffOptionsAsync(gymCode, form.AssignedStaffId, cancellationToken);
    }

    private async Task<MaintenanceTaskResponse?> FindTaskAsync(string gymCode, Guid taskId, CancellationToken cancellationToken)
    {
        var tasks = await maintenanceWorkflowService.GetMaintenanceTasksAsync(gymCode, cancellationToken: cancellationToken);
        return tasks.FirstOrDefault(task => task.Id == taskId);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildStaffOptionsAsync(string gymCode, Guid? selectedStaffId, CancellationToken cancellationToken)
    {
        var staff = await staffWorkflowService.GetStaffAsync(gymCode, cancellationToken: cancellationToken);
        return staff
            .Where(member => member.Status == StaffStatus.Active)
            .Select(member => new SelectListItem(
                member.FullName,
                member.Id.ToString(),
                selectedStaffId.HasValue && member.Id == selectedStaffId.Value))
            .ToArray();
    }

    private static DateTime ToUtc(DateTime localWallClock) =>
        DateTime.SpecifyKind(localWallClock, DateTimeKind.Local).ToUniversalTime();
}

public sealed class AdminSessionsPageService(
    IAdminSessionsQueryService sessionsQueryService,
    ITrainingWorkflowService trainingWorkflowService,
    IStaffWorkflowService staffWorkflowService) : IAdminSessionsPageService
{
    public async Task<AdminSessionsPageViewModel> BuildAsync(Guid gymId, string gymCode, CancellationToken cancellationToken = default)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var sessions = await sessionsQueryService.GetSessionsAsync(gymId, cancellationToken);

        return new AdminSessionsPageViewModel
        {
            GymCode = gymCode,
            Sessions = sessions
                .Select(row => new AdminSessionSummaryViewModel
                {
                    Id = row.Id,
                    Name = row.Name.Translate(culture) ?? row.Name.ToString(),
                    StartAtUtc = row.StartAtUtc,
                    EndAtUtc = row.EndAtUtc,
                    Capacity = row.Capacity,
                    BookingCount = row.BookingCount,
                    Status = row.Status,
                    TrainerNames = string.Join(", ", row.TrainerNames)
                })
                .ToArray()
        };
    }

    public async Task<AdminSessionFormViewModel> BuildCreateFormAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var start = RoundToNextHour(DateTime.Now);
        var form = new AdminSessionFormViewModel
        {
            GymCode = gymCode,
            StartAt = start,
            EndAt = start.AddHours(1),
            Capacity = 10,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Draft
        };

        await PopulateOptionsAsync(gymCode, form, cancellationToken);
        return form;
    }

    public async Task<AdminSessionFormViewModel?> GetEditFormAsync(string gymCode, Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await trainingWorkflowService.GetSessionAsync(gymCode, sessionId, cancellationToken);
            var form = new AdminSessionFormViewModel
            {
                Id = session.Id,
                GymCode = gymCode,
                CategoryId = session.CategoryId,
                Name = session.Name,
                Description = session.Description,
                StartAt = ToLocal(session.StartAtUtc),
                EndAt = ToLocal(session.EndAtUtc),
                Capacity = session.Capacity,
                BasePrice = session.BasePrice,
                CurrencyCode = session.CurrencyCode,
                Status = session.Status,
                TrainerStaffId = session.TrainerStaffId
            };

            await PopulateOptionsAsync(gymCode, form, cancellationToken);
            return form;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    public async Task<AdminSessionDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await trainingWorkflowService.GetSessionAsync(gymCode, sessionId, cancellationToken);
            return new AdminSessionDeleteViewModel
            {
                Id = session.Id,
                GymCode = gymCode,
                Name = session.Name,
                StartAtUtc = session.StartAtUtc,
                EndAtUtc = session.EndAtUtc,
                Status = session.Status
            };
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    public async Task<AdminSessionOperationResult> CreateAsync(string gymCode, AdminSessionFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, null, ToUpsertRequest(form), cancellationToken);
            return AdminSessionOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminSessionOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminSessionOperationResult.NotFound;
        }
    }

    public async Task<AdminSessionOperationResult> UpdateAsync(string gymCode, Guid sessionId, AdminSessionFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await trainingWorkflowService.UpsertTrainingSessionAsync(gymCode, sessionId, ToUpsertRequest(form), cancellationToken);
            return AdminSessionOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminSessionOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminSessionOperationResult.NotFound;
        }
    }

    public async Task<AdminSessionOperationResult> DeleteAsync(string gymCode, Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await trainingWorkflowService.DeleteSessionAsync(gymCode, sessionId, cancellationToken);
            return AdminSessionOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminSessionOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminSessionOperationResult.NotFound;
        }
    }

    public async Task PopulateOptionsAsync(string gymCode, AdminSessionFormViewModel form, CancellationToken cancellationToken = default)
    {
        var categories = await trainingWorkflowService.GetCategoriesAsync(gymCode, cancellationToken);
        form.CategoryOptions = categories
            .Select(category => new SelectListItem(category.Name, category.Id.ToString(), category.Id == form.CategoryId))
            .ToArray();

        var staff = await staffWorkflowService.GetStaffAsync(gymCode, cancellationToken: cancellationToken);
        form.TrainerOptions = staff
            .Where(member => member.Status == StaffStatus.Active)
            .Select(member => new SelectListItem(
                member.FullName,
                member.Id.ToString(),
                form.TrainerStaffId.HasValue && member.Id == form.TrainerStaffId.Value))
            .ToArray();
    }

    private static TrainingSessionUpsertRequest ToUpsertRequest(AdminSessionFormViewModel form) =>
        new()
        {
            CategoryId = form.CategoryId,
            Name = form.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
            StartAtUtc = ToUtc(form.StartAt),
            EndAtUtc = ToUtc(form.EndAt),
            Capacity = form.Capacity,
            BasePrice = form.BasePrice,
            CurrencyCode = string.IsNullOrWhiteSpace(form.CurrencyCode) ? "EUR" : form.CurrencyCode.Trim().ToUpperInvariant(),
            Status = form.Status,
            TrainerStaffId = form.TrainerStaffId
        };

    private static DateTime ToUtc(DateTime localWallClock) =>
        DateTime.SpecifyKind(localWallClock, DateTimeKind.Local).ToUniversalTime();

    private static DateTime ToLocal(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

    private static DateTime RoundToNextHour(DateTime value) =>
        new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Kind).AddHours(1);
}

public sealed class AdminMembershipsPageService(
    IMembershipService membershipService,
    IMembershipPackageService membershipPackageService,
    IMemberWorkflowService memberWorkflowService) : IAdminMembershipsPageService
{
    public async Task<AdminMembershipsPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var packages = (await membershipPackageService.GetPackagesAsync(gymCode, cancellationToken))
            .OrderBy(package => package.BasePrice)
            .Select(package => new MembershipPackageSummaryViewModel
            {
                Name = package.Name,
                BasePrice = package.BasePrice,
                CurrencyCode = package.CurrencyCode,
                IsTrainingFree = package.IsTrainingFree,
                TrainingDiscountPercent = package.TrainingDiscountPercent
            })
            .ToArray();

        var activeMemberships = (await membershipService.GetActiveMembershipSummariesAsync(gymCode, cancellationToken))
            .Select(membership => new ActiveMembershipSummaryViewModel
            {
                Id = membership.Id,
                MemberName = membership.MemberName,
                PackageName = membership.PackageName,
                StartDate = membership.StartDate,
                EndDate = membership.EndDate,
                Status = membership.Status
            })
            .ToArray();

        return new AdminMembershipsPageViewModel
        {
            GymCode = gymCode,
            Packages = packages,
            ActiveMemberships = activeMemberships
        };
    }

    public async Task<AdminMembershipSellFormViewModel> BuildSellFormAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var form = new AdminMembershipSellFormViewModel
        {
            GymCode = gymCode,
            RequestedStartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        };

        await PopulateSellOptionsAsync(gymCode, form, cancellationToken);
        return form;
    }

    public async Task<AdminMembershipEditFormViewModel?> GetEditFormAsync(string gymCode, Guid membershipId, CancellationToken cancellationToken = default)
    {
        var membership = await FindMembershipAsync(gymCode, membershipId, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        var memberNames = await BuildMemberNameLookupAsync(gymCode, cancellationToken);
        var form = new AdminMembershipEditFormViewModel
        {
            Id = membership.Id,
            GymCode = gymCode,
            MemberName = memberNames.GetValueOrDefault(membership.MemberId, string.Empty),
            MembershipPackageId = membership.MembershipPackageId,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status
        };

        await PopulateEditOptionsAsync(gymCode, form, cancellationToken);
        return form;
    }

    public async Task<AdminMembershipDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid membershipId, CancellationToken cancellationToken = default)
    {
        var membership = await FindMembershipAsync(gymCode, membershipId, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        var memberNames = await BuildMemberNameLookupAsync(gymCode, cancellationToken);
        var packageNames = await BuildPackageNameLookupAsync(gymCode, cancellationToken);

        return new AdminMembershipDeleteViewModel
        {
            Id = membership.Id,
            GymCode = gymCode,
            MemberName = memberNames.GetValueOrDefault(membership.MemberId, string.Empty),
            PackageName = packageNames.GetValueOrDefault(membership.MembershipPackageId, string.Empty),
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status
        };
    }

    public async Task<AdminMembershipOperationResult> SellAsync(string gymCode, AdminMembershipSellFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await membershipService.SellMembershipAsync(gymCode, new SellMembershipRequest
            {
                MemberId = form.MemberId,
                MembershipPackageId = form.MembershipPackageId,
                RequestedStartDate = form.RequestedStartDate,
                PaymentReference = string.IsNullOrWhiteSpace(form.PaymentReference) ? null : form.PaymentReference.Trim()
            }, cancellationToken);
            return AdminMembershipOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminMembershipOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminMembershipOperationResult.NotFound;
        }
    }

    public async Task<AdminMembershipOperationResult> UpdateAsync(string gymCode, Guid membershipId, AdminMembershipEditFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            var current = await FindMembershipAsync(gymCode, membershipId, cancellationToken);
            if (current is null)
            {
                return AdminMembershipOperationResult.NotFound;
            }

            await membershipService.UpdateMembershipAsync(gymCode, membershipId, new MembershipEditRequest
            {
                MembershipPackageId = form.MembershipPackageId,
                StartDate = form.StartDate,
                EndDate = form.EndDate
            }, cancellationToken);

            if (form.Status != current.Status)
            {
                await membershipService.UpdateMembershipStatusAsync(gymCode, membershipId, new MembershipStatusUpdateRequest
                {
                    Status = form.Status
                }, cancellationToken);
            }

            return AdminMembershipOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminMembershipOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminMembershipOperationResult.NotFound;
        }
    }

    public async Task<AdminMembershipOperationResult> DeleteAsync(string gymCode, Guid membershipId, CancellationToken cancellationToken = default)
    {
        try
        {
            await membershipService.DeleteMembershipAsync(gymCode, membershipId, cancellationToken);
            return AdminMembershipOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminMembershipOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminMembershipOperationResult.NotFound;
        }
    }

    public async Task PopulateSellOptionsAsync(string gymCode, AdminMembershipSellFormViewModel form, CancellationToken cancellationToken = default)
    {
        var members = await memberWorkflowService.GetMembersAsync(gymCode, cancellationToken: cancellationToken);
        form.MemberOptions = members
            .Select(member => new SelectListItem(
                $"{member.FullName} ({member.MemberCode})",
                member.Id.ToString(),
                member.Id == form.MemberId))
            .ToArray();

        var packages = await membershipPackageService.GetPackagesAsync(gymCode, cancellationToken);
        form.PackageOptions = packages
            .Select(package => new SelectListItem(
                $"{package.Name} - {package.BasePrice} {package.CurrencyCode}",
                package.Id.ToString(),
                package.Id == form.MembershipPackageId))
            .ToArray();
    }

    public async Task PopulateEditOptionsAsync(string gymCode, AdminMembershipEditFormViewModel form, CancellationToken cancellationToken = default)
    {
        var packages = await membershipPackageService.GetPackagesAsync(gymCode, cancellationToken);
        form.PackageOptions = packages
            .Select(package => new SelectListItem(
                $"{package.Name} - {package.BasePrice} {package.CurrencyCode}",
                package.Id.ToString(),
                package.Id == form.MembershipPackageId))
            .ToArray();
    }

    private async Task<MembershipResponse?> FindMembershipAsync(string gymCode, Guid membershipId, CancellationToken cancellationToken)
    {
        var memberships = await membershipService.GetMembershipsAsync(gymCode, cancellationToken: cancellationToken);
        return memberships.FirstOrDefault(membership => membership.Id == membershipId);
    }

    private async Task<Dictionary<Guid, string>> BuildMemberNameLookupAsync(string gymCode, CancellationToken cancellationToken)
    {
        var members = await memberWorkflowService.GetMembersAsync(gymCode, cancellationToken: cancellationToken);
        return members.ToDictionary(member => member.Id, member => member.FullName);
    }

    private async Task<Dictionary<Guid, string>> BuildPackageNameLookupAsync(string gymCode, CancellationToken cancellationToken)
    {
        var packages = await membershipPackageService.GetPackagesAsync(gymCode, cancellationToken);
        return packages.ToDictionary(package => package.Id, package => package.Name);
    }
}

public sealed class AdminMembersPageService(
    IMemberWorkflowService memberWorkflowService,
    IMemberAccountService memberAccountService) : IAdminMembersPageService
{
    public async Task<AdminMembersPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var members = await memberWorkflowService.GetMembersAsync(gymCode, cancellationToken: cancellationToken);
        var summaries = members
            .Select(member => new AdminMemberSummaryViewModel
            {
                Id = member.Id,
                MemberCode = member.MemberCode,
                FullName = member.FullName,
                Status = member.Status
            })
            .ToArray();

        return new AdminMembersPageViewModel
        {
            GymCode = gymCode,
            TotalCount = summaries.Length,
            ActiveCount = summaries.Count(member => member.Status == MemberStatus.Active),
            SuspendedCount = summaries.Count(member => member.Status == MemberStatus.Suspended),
            LeftCount = summaries.Count(member => member.Status == MemberStatus.Left),
            Members = summaries
        };
    }

    public async Task<AdminMemberFormViewModel?> GetEditFormAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            var member = await memberWorkflowService.GetMemberAsync(gymCode, memberId, cancellationToken);
            var email = await memberAccountService.GetLoginEmailByMemberAsync(gymCode, memberId, cancellationToken);
            return new AdminMemberFormViewModel
            {
                Id = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                MemberCode = member.MemberCode,
                PersonalCode = member.PersonalCode ?? string.Empty,
                DateOfBirth = member.DateOfBirth,
                Status = member.Status,
                Email = email,
                GymCode = gymCode
            };
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    public async Task<AdminMemberDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            var member = await memberWorkflowService.GetMemberAsync(gymCode, memberId, cancellationToken);
            return new AdminMemberDeleteViewModel
            {
                Id = member.Id,
                MemberCode = member.MemberCode,
                FullName = member.FullName,
                Status = member.Status,
                GymCode = gymCode
            };
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    public async Task<AdminMemberOperationResult> CreateAsync(string gymCode, AdminMemberFormViewModel form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.Email) || string.IsNullOrWhiteSpace(form.NewPassword))
        {
            return AdminMemberOperationResult.ValidationFailed(["A login email and an initial password are required when creating a member."]);
        }

        try
        {
            await memberWorkflowService.CreateMemberAsync(gymCode, ToUpsertRequest(form), cancellationToken);
            return AdminMemberOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminMemberOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminMemberOperationResult.NotFound;
        }
    }

    public async Task<AdminMemberOperationResult> UpdateAsync(string gymCode, Guid memberId, AdminMemberFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await memberWorkflowService.UpdateMemberAsync(gymCode, memberId, ToUpsertRequest(form), cancellationToken);

            if (!string.IsNullOrWhiteSpace(form.NewPassword))
            {
                await memberAccountService.SetPasswordByMemberAsync(gymCode, memberId, form.NewPassword, cancellationToken);
            }

            return AdminMemberOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminMemberOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminMemberOperationResult.NotFound;
        }
    }

    public async Task<AdminMemberOperationResult> DeleteAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            await memberWorkflowService.DeleteMemberAsync(gymCode, memberId, cancellationToken);
            return AdminMemberOperationResult.Success;
        }
        catch (NotFoundException)
        {
            return AdminMemberOperationResult.NotFound;
        }
    }

    private static MemberUpsertRequest ToUpsertRequest(AdminMemberFormViewModel form)
    {
        return new MemberUpsertRequest
        {
            FirstName = form.FirstName,
            LastName = form.LastName,
            MemberCode = form.MemberCode,
            PersonalCode = string.IsNullOrWhiteSpace(form.PersonalCode) ? null : form.PersonalCode,
            DateOfBirth = form.DateOfBirth,
            Status = form.Status,
            Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim(),
            Password = string.IsNullOrWhiteSpace(form.NewPassword) ? null : form.NewPassword
        };
    }
}

public enum AdminMembershipPackageOperationStatus
{
    Success,
    NotFound,
    ValidationFailed,
    Conflict
}

public sealed record AdminMembershipPackageOperationResult(
    AdminMembershipPackageOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminMembershipPackageOperationResult Success { get; } =
        new(AdminMembershipPackageOperationStatus.Success, Array.Empty<string>());

    public static AdminMembershipPackageOperationResult NotFound { get; } =
        new(AdminMembershipPackageOperationStatus.NotFound, Array.Empty<string>());

    public static AdminMembershipPackageOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminMembershipPackageOperationStatus.ValidationFailed, errors.ToArray());

    public static AdminMembershipPackageOperationResult Conflict(string error) =>
        new(AdminMembershipPackageOperationStatus.Conflict, new[] { error });
}

public interface IAdminMembershipPackagesPageService
{
    Task<AdminMembershipPackagesPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default);

    Task<AdminMembershipPackageFormViewModel?> GetEditFormAsync(string gymCode, Guid packageId, CancellationToken cancellationToken = default);

    Task<AdminMembershipPackageDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid packageId, CancellationToken cancellationToken = default);

    Task<AdminMembershipPackageOperationResult> CreateAsync(string gymCode, AdminMembershipPackageFormViewModel form, CancellationToken cancellationToken = default);

    Task<AdminMembershipPackageOperationResult> UpdateAsync(string gymCode, Guid packageId, AdminMembershipPackageFormViewModel form, CancellationToken cancellationToken = default);

    Task<AdminMembershipPackageOperationResult> DeleteAsync(string gymCode, Guid packageId, CancellationToken cancellationToken = default);
}

public sealed class AdminMembershipPackagesPageService(
    IMembershipPackageService membershipPackageService,
    IAuthorizationService authorizationService,
    IMembershipPackageRepository membershipPackageRepository) : IAdminMembershipPackagesPageService
{
    public async Task<AdminMembershipPackagesPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, SharedKernel.RoleNames.GymOwner, SharedKernel.RoleNames.GymAdmin);
        var packages = await membershipPackageRepository.ListByGymAsync(gymId, cancellationToken);

        var summaries = packages
            .OrderBy(package => package.BasePrice)
            .Select(package => new AdminMembershipPackageSummaryViewModel
            {
                Id = package.Id,
                Name = package.Name.ToString(),
                PackageType = package.PackageType,
                DurationValue = package.DurationValue,
                DurationUnit = package.DurationUnit,
                BasePrice = package.BasePrice,
                CurrencyCode = package.CurrencyCode,
                IsTrainingFree = package.IsTrainingFree,
                TrainingDiscountPercent = package.TrainingDiscountPercent
            })
            .ToArray();

        return new AdminMembershipPackagesPageViewModel
        {
            GymCode = gymCode,
            Packages = summaries
        };
    }

    public async Task<AdminMembershipPackageFormViewModel?> GetEditFormAsync(string gymCode, Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await FindPackageAsync(gymCode, packageId, cancellationToken);
        if (package is null)
        {
            return null;
        }

        return new AdminMembershipPackageFormViewModel
        {
            Id = package.Id,
            Name = package.Name.ToString(),
            PackageType = package.PackageType,
            DurationValue = package.DurationValue,
            DurationUnit = package.DurationUnit,
            BasePrice = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            TrainingDiscountPercent = package.TrainingDiscountPercent,
            IsTrainingFree = package.IsTrainingFree,
            Description = package.Description?.ToString(),
            GymCode = gymCode
        };
    }

    public async Task<AdminMembershipPackageDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await FindPackageAsync(gymCode, packageId, cancellationToken);
        if (package is null)
        {
            return null;
        }

        return new AdminMembershipPackageDeleteViewModel
        {
            Id = package.Id,
            Name = package.Name.ToString(),
            BasePrice = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            GymCode = gymCode
        };
    }

    public async Task<AdminMembershipPackageOperationResult> CreateAsync(string gymCode, AdminMembershipPackageFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await membershipPackageService.CreatePackageAsync(gymCode, ToUpsertRequest(form), cancellationToken);
            return AdminMembershipPackageOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminMembershipPackageOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminMembershipPackageOperationResult.NotFound;
        }
    }

    public async Task<AdminMembershipPackageOperationResult> UpdateAsync(string gymCode, Guid packageId, AdminMembershipPackageFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await membershipPackageService.UpdatePackageAsync(gymCode, packageId, ToUpsertRequest(form), cancellationToken);
            return AdminMembershipPackageOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminMembershipPackageOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminMembershipPackageOperationResult.NotFound;
        }
    }

    public async Task<AdminMembershipPackageOperationResult> DeleteAsync(string gymCode, Guid packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await membershipPackageService.DeletePackageAsync(gymCode, packageId, cancellationToken);
            return AdminMembershipPackageOperationResult.Success;
        }
        catch (NotFoundException)
        {
            return AdminMembershipPackageOperationResult.NotFound;
        }
        catch (ConflictAppException exception)
        {
            return AdminMembershipPackageOperationResult.Conflict(exception.Message);
        }
    }

    private async Task<App.Domain.Entities.MembershipPackage?> FindPackageAsync(string gymCode, Guid packageId, CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, SharedKernel.RoleNames.GymOwner, SharedKernel.RoleNames.GymAdmin);
        return await membershipPackageRepository.FindAsync(gymId, packageId, cancellationToken);
    }

    private static MembershipPackageUpsertRequest ToUpsertRequest(AdminMembershipPackageFormViewModel form)
    {
        return new MembershipPackageUpsertRequest
        {
            Name = form.Name,
            PackageType = form.PackageType,
            DurationValue = form.DurationValue,
            DurationUnit = form.DurationUnit,
            BasePrice = form.BasePrice,
            CurrencyCode = form.CurrencyCode,
            TrainingDiscountPercent = form.TrainingDiscountPercent,
            IsTrainingFree = form.IsTrainingFree,
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description
        };
    }
}

public enum AdminTrainingCategoryOperationStatus
{
    Success,
    NotFound,
    ValidationFailed,
    Conflict
}

public sealed record AdminTrainingCategoryOperationResult(
    AdminTrainingCategoryOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminTrainingCategoryOperationResult Success { get; } =
        new(AdminTrainingCategoryOperationStatus.Success, Array.Empty<string>());

    public static AdminTrainingCategoryOperationResult NotFound { get; } =
        new(AdminTrainingCategoryOperationStatus.NotFound, Array.Empty<string>());

    public static AdminTrainingCategoryOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminTrainingCategoryOperationStatus.ValidationFailed, errors.ToArray());

    public static AdminTrainingCategoryOperationResult Conflict(string error) =>
        new(AdminTrainingCategoryOperationStatus.Conflict, new[] { error });
}

public interface IAdminTrainingCategoriesPageService
{
    Task<AdminTrainingCategoriesPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default);

    Task<AdminTrainingCategoryFormViewModel?> GetEditFormAsync(string gymCode, Guid categoryId, CancellationToken cancellationToken = default);

    Task<AdminTrainingCategoryDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid categoryId, CancellationToken cancellationToken = default);

    Task<AdminTrainingCategoryOperationResult> CreateAsync(string gymCode, AdminTrainingCategoryFormViewModel form, CancellationToken cancellationToken = default);

    Task<AdminTrainingCategoryOperationResult> UpdateAsync(string gymCode, Guid categoryId, AdminTrainingCategoryFormViewModel form, CancellationToken cancellationToken = default);

    Task<AdminTrainingCategoryOperationResult> DeleteAsync(string gymCode, Guid categoryId, CancellationToken cancellationToken = default);
}

public sealed class AdminTrainingCategoriesPageService(
    ITrainingWorkflowService trainingWorkflowService,
    IAuthorizationService authorizationService,
    ITrainingCategoryRepository trainingCategoryRepository) : IAdminTrainingCategoriesPageService
{
    public async Task<AdminTrainingCategoriesPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, SharedKernel.RoleNames.GymOwner, SharedKernel.RoleNames.GymAdmin);
        var categories = await trainingCategoryRepository.ListByGymAsync(gymId, cancellationToken);

        var summaries = categories
            .OrderBy(category => category.Name.ToString(), StringComparer.CurrentCulture)
            .Select(category =>
            {
                return new AdminTrainingCategorySummaryViewModel
                {
                    Id = category.Id,
                    Name = category.Name.ToString(),
                    AlternateNames = GetAlternateTranslations(category.Name),
                    Description = category.Description?.ToString(),
                    AlternateDescriptions = GetAlternateTranslations(category.Description)
                };
            })
            .ToArray();

        return new AdminTrainingCategoriesPageViewModel
        {
            GymCode = gymCode,
            Categories = summaries
        };
    }

    public async Task<AdminTrainingCategoryFormViewModel?> GetEditFormAsync(string gymCode, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(gymCode, categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        return new AdminTrainingCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name.ToString(),
            Description = category.Description?.ToString(),
            AlternateNames = GetAlternateTranslations(category.Name),
            AlternateDescriptions = GetAlternateTranslations(category.Description),
            GymCode = gymCode
        };
    }

    public async Task<AdminTrainingCategoryDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(gymCode, categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        return new AdminTrainingCategoryDeleteViewModel
        {
            Id = category.Id,
            Name = category.Name.Translate(CultureInfo.CurrentUICulture.Name) ?? category.Name.ToString(),
            AlternateNames = Array.Empty<string>(),
            Description = category.Description?.Translate(CultureInfo.CurrentUICulture.Name) ?? category.Description?.ToString(),
            GymCode = gymCode
        };
    }

    public async Task<AdminTrainingCategoryOperationResult> CreateAsync(string gymCode, AdminTrainingCategoryFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await trainingWorkflowService.CreateCategoryAsync(gymCode, ToUpsertRequest(form), cancellationToken);
            return AdminTrainingCategoryOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminTrainingCategoryOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminTrainingCategoryOperationResult.NotFound;
        }
    }

    public async Task<AdminTrainingCategoryOperationResult> UpdateAsync(string gymCode, Guid categoryId, AdminTrainingCategoryFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await trainingWorkflowService.UpdateCategoryAsync(gymCode, categoryId, ToUpsertRequest(form), cancellationToken);
            return AdminTrainingCategoryOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminTrainingCategoryOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminTrainingCategoryOperationResult.NotFound;
        }
    }

    public async Task<AdminTrainingCategoryOperationResult> DeleteAsync(string gymCode, Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            await trainingWorkflowService.DeleteCategoryAsync(gymCode, categoryId, cancellationToken);
            return AdminTrainingCategoryOperationResult.Success;
        }
        catch (NotFoundException)
        {
            return AdminTrainingCategoryOperationResult.NotFound;
        }
        catch (ConflictAppException exception)
        {
            return AdminTrainingCategoryOperationResult.Conflict(exception.Message);
        }
    }

    private async Task<App.Domain.Entities.TrainingCategory?> FindCategoryAsync(string gymCode, Guid categoryId, CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, SharedKernel.RoleNames.GymOwner, SharedKernel.RoleNames.GymAdmin);
        return await trainingCategoryRepository.FindAsync(gymId, categoryId, cancellationToken);
    }

    private static IReadOnlyCollection<string> GetAlternateTranslations(Base.Domain.LangStr? value)
    {
        if (value is null)
        {
            return Array.Empty<string>();
        }

        var primary = value.ToString();
        return value.Values
            .Where(translation => !string.Equals(translation, primary, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static TrainingCategoryUpsertRequest ToUpsertRequest(AdminTrainingCategoryFormViewModel form)
    {
        return new TrainingCategoryUpsertRequest
        {
            Name = form.Name?.Trim() ?? string.Empty,
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim()
        };
    }
}

public enum AdminStaffOperationStatus
{
    Success,
    ValidationFailed,
    NotFound
}

public sealed record AdminStaffOperationResult(
    AdminStaffOperationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AdminStaffOperationResult Success { get; } =
        new(AdminStaffOperationStatus.Success, Array.Empty<string>());

    public static AdminStaffOperationResult NotFound { get; } =
        new(AdminStaffOperationStatus.NotFound, Array.Empty<string>());

    public static AdminStaffOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new(AdminStaffOperationStatus.ValidationFailed, errors.ToArray());
}

public interface IAdminStaffPageService
{
    Task<AdminStaffPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default);
    Task<AdminStaffFormViewModel?> GetEditFormAsync(string gymCode, Guid staffId, CancellationToken cancellationToken = default);
    Task<AdminStaffDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid staffId, CancellationToken cancellationToken = default);
    Task<AdminStaffOperationResult> CreateAsync(string gymCode, AdminStaffFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminStaffOperationResult> UpdateAsync(string gymCode, Guid staffId, AdminStaffFormViewModel form, CancellationToken cancellationToken = default);
    Task<AdminStaffOperationResult> DeleteAsync(string gymCode, Guid staffId, CancellationToken cancellationToken = default);
}

public sealed class AdminStaffPageService(IStaffWorkflowService staffWorkflowService) : IAdminStaffPageService
{
    public async Task<AdminStaffPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var staff = await staffWorkflowService.GetStaffAsync(gymCode, cancellationToken: cancellationToken);
        var summaries = staff
            .Select(member => new AdminStaffSummaryViewModel
            {
                Id = member.Id,
                StaffCode = member.StaffCode,
                FullName = member.FullName,
                Status = member.Status
            })
            .ToArray();

        return new AdminStaffPageViewModel
        {
            GymCode = gymCode,
            TotalCount = summaries.Length,
            ActiveCount = summaries.Count(member => member.Status == StaffStatus.Active),
            SuspendedCount = summaries.Count(member => member.Status == StaffStatus.Suspended),
            InactiveCount = summaries.Count(member => member.Status == StaffStatus.Inactive),
            Staff = summaries
        };
    }

    public async Task<AdminStaffFormViewModel?> GetEditFormAsync(string gymCode, Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await FindSummaryAsync(gymCode, staffId, cancellationToken);
        if (staff is null)
        {
            return null;
        }

        var (firstName, lastName) = SplitFullName(staff.FullName);
        return new AdminStaffFormViewModel
        {
            Id = staff.Id,
            FirstName = firstName,
            LastName = lastName,
            StaffCode = staff.StaffCode,
            Status = staff.Status,
            GymCode = gymCode
        };
    }

    public async Task<AdminStaffDeleteViewModel?> GetDeleteViewAsync(string gymCode, Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await FindSummaryAsync(gymCode, staffId, cancellationToken);
        if (staff is null)
        {
            return null;
        }

        return new AdminStaffDeleteViewModel
        {
            Id = staff.Id,
            StaffCode = staff.StaffCode,
            FullName = staff.FullName,
            Status = staff.Status,
            GymCode = gymCode
        };
    }

    public async Task<AdminStaffOperationResult> CreateAsync(string gymCode, AdminStaffFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await staffWorkflowService.CreateStaffAsync(gymCode, ToUpsertRequest(form), cancellationToken);
            return AdminStaffOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminStaffOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminStaffOperationResult.NotFound;
        }
    }

    public async Task<AdminStaffOperationResult> UpdateAsync(string gymCode, Guid staffId, AdminStaffFormViewModel form, CancellationToken cancellationToken = default)
    {
        try
        {
            await staffWorkflowService.UpdateStaffAsync(gymCode, staffId, ToUpsertRequest(form), cancellationToken);
            return AdminStaffOperationResult.Success;
        }
        catch (ValidationAppException exception)
        {
            return AdminStaffOperationResult.ValidationFailed(exception.Errors);
        }
        catch (NotFoundException)
        {
            return AdminStaffOperationResult.NotFound;
        }
    }

    public async Task<AdminStaffOperationResult> DeleteAsync(string gymCode, Guid staffId, CancellationToken cancellationToken = default)
    {
        try
        {
            await staffWorkflowService.DeleteStaffAsync(gymCode, staffId, cancellationToken);
            return AdminStaffOperationResult.Success;
        }
        catch (NotFoundException)
        {
            return AdminStaffOperationResult.NotFound;
        }
    }

    private async Task<StaffResponse?> FindSummaryAsync(string gymCode, Guid staffId, CancellationToken cancellationToken)
    {
        var staff = await staffWorkflowService.GetStaffAsync(gymCode, cancellationToken: cancellationToken);
        return staff.FirstOrDefault(member => member.Id == staffId);
    }

    private static StaffUpsertRequest ToUpsertRequest(AdminStaffFormViewModel form)
    {
        return new StaffUpsertRequest
        {
            FirstName = form.FirstName.Trim(),
            LastName = form.LastName.Trim(),
            StaffCode = form.StaffCode.Trim(),
            Status = form.Status
        };
    }

    private static (string FirstName, string LastName) SplitFullName(string fullName)
    {
        var trimmed = fullName.Trim();
        var space = trimmed.IndexOf(' ');
        return space < 0
            ? (trimmed, string.Empty)
            : (trimmed[..space], trimmed[(space + 1)..]);
    }
}

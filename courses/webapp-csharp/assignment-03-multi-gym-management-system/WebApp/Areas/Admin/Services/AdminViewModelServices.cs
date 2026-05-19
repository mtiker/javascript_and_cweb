using System.Globalization;
using App.BLL.Exceptions;
using App.DAL.Contracts.Persistence;
using App.BLL.Contracts.Services;
using App.BLL.Contracts.Services.Admin;
using App.Domain.Enums;
using App.DTO.v1.Members;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.TrainingCategories;
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

public sealed class AdminOperationsPageService(IAdminOperationsQueryService operationsQueryService) : IAdminOperationsPageService
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
                    AssetTag = row.AssetTag,
                    ModelName = row.ModelName?.Translate(culture) ?? string.Empty,
                    Status = row.Status
                })
                .ToArray(),
            MaintenanceTasks = snapshot.MaintenanceTasks
                .Select(row => new MaintenanceSummaryViewModel
                {
                    AssetTag = row.AssetTag,
                    TaskType = row.TaskType,
                    Status = row.Status,
                    AssignedTo = row.AssignedTo,
                    DueAtUtc = row.DueAtUtc
                })
                .ToArray()
        };
    }
}

public sealed class AdminSessionsPageService(IAdminSessionsQueryService sessionsQueryService) : IAdminSessionsPageService
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
}

public sealed class AdminMembersPageService(IMemberWorkflowService memberWorkflowService) : IAdminMembersPageService
{
    public async Task<AdminMembersPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var members = await memberWorkflowService.GetMembersAsync(gymCode, cancellationToken);
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
            return new AdminMemberFormViewModel
            {
                Id = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                MemberCode = member.MemberCode,
                PersonalCode = member.PersonalCode ?? string.Empty,
                DateOfBirth = member.DateOfBirth,
                Status = member.Status,
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
            Status = form.Status
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
    IAppUnitOfWork unitOfWork) : IAdminMembershipPackagesPageService
{
    public async Task<AdminMembershipPackagesPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var packages = await unitOfWork.MembershipPackages.ListByGymAsync(gymId, cancellationToken);

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
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        return await unitOfWork.MembershipPackages.FindAsync(gymId, packageId, cancellationToken);
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
    IAppUnitOfWork unitOfWork) : IAdminTrainingCategoriesPageService
{
    public async Task<AdminTrainingCategoriesPageViewModel> BuildIndexAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        var categories = await unitOfWork.TrainingCategories.ListByGymAsync(gymId, cancellationToken);

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
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin);
        return await unitOfWork.TrainingCategories.FindAsync(gymId, categoryId, cancellationToken);
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

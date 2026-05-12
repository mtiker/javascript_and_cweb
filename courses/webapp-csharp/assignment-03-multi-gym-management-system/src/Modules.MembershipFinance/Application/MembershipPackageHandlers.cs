using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using App.DTO.v1.MembershipPackages;
using BuildingBlocks.Mediator;
using Modules.MembershipFinance.Contracts;

namespace Modules.MembershipFinance.Application;

internal sealed class ListMembershipPackagesQueryHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper)
    : IRequestHandler<ListMembershipPackagesQuery, IReadOnlyCollection<MembershipPackageResponse>>
{
    public async Task<IReadOnlyCollection<MembershipPackageResponse>> HandleAsync(
        ListMembershipPackagesQuery request,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var packages = await unitOfWork.MembershipPackages.ListByGymAsync(gymId, cancellationToken);
        return mapper.ToPackageResponses(packages);
    }
}

internal sealed class CreateMembershipPackageCommandHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper)
    : IRequestHandler<CreateMembershipPackageCommand, MembershipPackageResponse>
{
    public async Task<MembershipPackageResponse> HandleAsync(
        CreateMembershipPackageCommand request,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin);

        var normalized = MembershipPackageWorkflow.NormalizeRequest(request.Request);
        var package = new MembershipPackage
        {
            GymId = gymId,
            Name = mapper.ToLangStr(normalized.Name!),
            PackageType = normalized.PackageType,
            DurationValue = normalized.DurationValue,
            DurationUnit = normalized.DurationUnit,
            BasePrice = normalized.BasePrice,
            CurrencyCode = normalized.CurrencyCode!,
            TrainingDiscountPercent = normalized.TrainingDiscountPercent,
            IsTrainingFree = normalized.IsTrainingFree,
            Description = normalized.Description is null ? null : mapper.ToLangStr(normalized.Description)
        };

        await unitOfWork.MembershipPackages.AddAsync(package, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.ToPackageResponse(package);
    }
}

internal sealed class UpdateMembershipPackageCommandHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper)
    : IRequestHandler<UpdateMembershipPackageCommand, MembershipPackageResponse>
{
    public async Task<MembershipPackageResponse> HandleAsync(
        UpdateMembershipPackageCommand request,
        CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin);

        var normalized = MembershipPackageWorkflow.NormalizeRequest(request.Request);
        var package = await unitOfWork.MembershipPackages.FindAsync(gymId, request.PackageId, cancellationToken)
            ?? throw new NotFoundException("Membership package was not found.");

        package.Name = mapper.ToLangStr(normalized.Name!);
        package.PackageType = normalized.PackageType;
        package.DurationValue = normalized.DurationValue;
        package.DurationUnit = normalized.DurationUnit;
        package.BasePrice = normalized.BasePrice;
        package.CurrencyCode = normalized.CurrencyCode!;
        package.TrainingDiscountPercent = normalized.TrainingDiscountPercent;
        package.IsTrainingFree = normalized.IsTrainingFree;
        package.Description = normalized.Description is null ? null : mapper.ToLangStr(normalized.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.ToPackageResponse(package);
    }
}

internal sealed class DeleteMembershipPackageCommandHandler(
    IAppUnitOfWork unitOfWork,
    IAuthorizationService authorizationService)
    : IRequestHandler<DeleteMembershipPackageCommand>
{
    public async Task HandleAsync(DeleteMembershipPackageCommand request, CancellationToken cancellationToken)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            request.GymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin);

        var package = await unitOfWork.MembershipPackages.FindAsync(gymId, request.PackageId, cancellationToken)
            ?? throw new NotFoundException("Membership package was not found.");

        if (await unitOfWork.MembershipPackages.IsUsedAsync(gymId, request.PackageId, cancellationToken))
        {
            throw new ConflictAppException("Membership package is already used by memberships. Deactivate the package instead of deleting it.");
        }

        unitOfWork.MembershipPackages.Remove(package);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal static class MembershipPackageWorkflow
{
    public static MembershipPackageUpsertRequest NormalizeRequest(MembershipPackageUpsertRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Package name is required.");
        }

        if (!Enum.IsDefined(request.PackageType))
        {
            errors.Add("Package type is invalid.");
        }

        if (request.DurationValue <= 0)
        {
            errors.Add("Duration value must be greater than zero.");
        }

        if (!Enum.IsDefined(request.DurationUnit))
        {
            errors.Add("Duration unit is invalid.");
        }

        if (request.BasePrice < 0)
        {
            errors.Add("Base price must be zero or greater.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            errors.Add("Currency code is required.");
        }
        else if (request.CurrencyCode.Trim().Length != 3 || !request.CurrencyCode.Trim().All(char.IsLetter))
        {
            errors.Add("Currency code must be a three-letter ISO currency code.");
        }

        if (request.TrainingDiscountPercent is < 0 or > 100)
        {
            errors.Add("Training discount must be between 0 and 100.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }

        return new MembershipPackageUpsertRequest
        {
            Name = request.Name!.Trim(),
            PackageType = request.PackageType,
            DurationValue = request.DurationValue,
            DurationUnit = request.DurationUnit,
            BasePrice = request.BasePrice,
            CurrencyCode = request.CurrencyCode!.Trim().ToUpperInvariant(),
            TrainingDiscountPercent = request.TrainingDiscountPercent,
            IsTrainingFree = request.IsTrainingFree,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
    }
}

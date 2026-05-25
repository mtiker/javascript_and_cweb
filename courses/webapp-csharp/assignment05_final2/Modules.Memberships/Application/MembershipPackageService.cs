using App.BLL.Contracts.Infrastructure;
using App.BLL.Contracts.Services;
using SharedKernel.Exceptions;
using Modules.Memberships.Application.Mappers;
using SharedKernel;
using App.Domain.Entities;
using Modules.Memberships.Application.Persistence;
using Shared.Contracts.Dtos.v1.MembershipPackages;

namespace Modules.Memberships.Application;

public class MembershipPackageService(
    IAppDbContext dbContext,
    IMembershipPackageRepository membershipPackageRepository,
    IAuthorizationService authorizationService,
    IMembershipFinanceMapper mapper) : IMembershipPackageService
{
    public async Task<IReadOnlyCollection<MembershipPackageResponse>> GetPackagesAsync(string gymCode, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(
            gymCode,
            cancellationToken,
            RoleNames.GymOwner,
            RoleNames.GymAdmin,
            RoleNames.Member);

        var packages = await membershipPackageRepository.ListByGymAsync(gymId, cancellationToken);
        return mapper.ToPackageResponses(packages);
    }

    public async Task<MembershipPackageResponse> CreatePackageAsync(string gymCode, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var normalized = NormalizeRequest(request);

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
            Description = normalized.Description == null ? null : mapper.ToLangStr(normalized.Description)
        };

        await membershipPackageRepository.AddAsync(package, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return mapper.ToPackageResponse(package);
    }

    public async Task<MembershipPackageResponse> UpdatePackageAsync(string gymCode, Guid id, MembershipPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);
        var normalized = NormalizeRequest(request);

        var package = await membershipPackageRepository.FindAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        package.Name = mapper.ToLangStr(normalized.Name!);
        package.PackageType = normalized.PackageType;
        package.DurationValue = normalized.DurationValue;
        package.DurationUnit = normalized.DurationUnit;
        package.BasePrice = normalized.BasePrice;
        package.CurrencyCode = normalized.CurrencyCode!;
        package.TrainingDiscountPercent = normalized.TrainingDiscountPercent;
        package.IsTrainingFree = normalized.IsTrainingFree;
        package.Description = normalized.Description == null ? null : mapper.ToLangStr(normalized.Description);

        await dbContext.SaveChangesAsync(cancellationToken);

        return mapper.ToPackageResponse(package);
    }

    public async Task DeletePackageAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
    {
        var gymId = await authorizationService.EnsureTenantAccessAsync(gymCode, cancellationToken, RoleNames.GymOwner, RoleNames.GymAdmin);

        var package = await membershipPackageRepository.FindAsync(gymId, id, cancellationToken)
                      ?? throw new NotFoundException("Membership package was not found.");

        if (await membershipPackageRepository.IsUsedAsync(gymId, id, cancellationToken))
        {
            throw new ConflictAppException("Membership package is already used by memberships. Deactivate the package instead of deleting it.");
        }

        membershipPackageRepository.Remove(package);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MembershipPackageUpsertRequest NormalizeRequest(MembershipPackageUpsertRequest request)
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
